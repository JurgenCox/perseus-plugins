#!/usr/bin/env python3
import sys, os, uuid
from itertools import takewhile
from perseuspy import pd, nx, write_networks, read_networks
import numpy as np
import perseuspy.parameters as params
import phos.data.network as networker
import phos.algo.anat as anat
import phos.algo.activity as activity
from joblib import Parallel, delayed

def split_and_stack(frame, colname):
    mask = frame[colname].isnull()
    masked_frame=frame[~mask]
    mapping = pd.DataFrame([{i: v for i,v in enumerate(x)} for x in masked_frame[colname]],
                             index=masked_frame.index).stack().reset_index(1,drop=True)
    mapping.name = colname
    return frame.drop(colname, axis=1).join(mapping)

def aggregate_scores(scores, additional_columns):
    """Aggregate all scores into one table.
    :param scores: list of pd.DataFrame with scores and additional 'Column Name' column
    :param additional_columns: include all metrics reported by photon, or just score and significance
    """
    _score = pd.concat(scores)
    score = (_score.pivot_table('score_empiric', 'GeneID', 'Column Name')
            .rename(columns = lambda x: '{} Signaling score'.format(x)))
    num_columns = [] if not additional_columns else ['t',
            'p_greater', 'p_lesser', 'p_twosided', 'q_greater', 'q_lesser', 'q_twosided']
    for num_column in num_columns:
        values = (_score
                .pivot_table(num_column, 'GeneID', 'Column Name')
                .rename(columns = lambda x : '{} {}'.format(x, num_column))
                .apply(lambda col: col.astype(float)))
        score = score.join(values)
    cat_columns = ['Significant'] if not additional_columns else ['rej_greater',
            'rej_lesser', 'rej_twosided', 'Significant']
    for cat_column in cat_columns:
        significant = (_score
                .pivot_table(cat_column, 'GeneID', 'Column Name')
                .rename(columns = lambda x : '{} {}'.format(x, cat_column))
                .fillna('')
                .apply(lambda col: col.astype('category')))
        score = score.join(significant)
    score = score.reset_index().rename(columns = {'GeneID': 'Node'})
    return score

def run(data_column, confidence_column, name, node_table, edge_table, run_anat, anchor, top_n_terminals, parameters):
    print('Calculating scores for', data_column, flush=True)
    if 'GeneID' in node_table.columns:
        __exp = node_table.drop('GeneID', 1)
    else:
        __exp = node_table
    if data_column != 'avg' and 'avg' in node_table.columns:
        __exp = __exp.drop('avg', 1)
    _exp = __exp.rename(columns = {'Node': 'GeneID', data_column: 'avg'})[['GeneID', 'avg']]
    exp = split_and_stack(_exp, 'avg').dropna()
    if confidence_column == 'Use constant value':
        edge_table['Use constant value'] = 1
    network = edge_table.rename(columns = {'Source': 'kin', 'Target': 'sub', confidence_column: 'confidence'})
    score = activity.empiric(exp, network, **parameters['activity']).assign(**{'Column Name': data_column})
    terminal = score[score['Significant']].sort_values(by='score_empiric').head(top_n_terminals)['GeneID'].astype(str)
    if not run_anat:
        subnet = None
    elif len(terminal) + (0 if anchor is None else 1) < 2:
        print('Cannot create {}anchored network with {} terminals'.format('un' if anchor is None else '', len(terminal)))
        subnet = None
    else:
        print('Querying ANAT with {} terminals for {}'.format(len(terminal), data_column), flush=True)
        session_id = 'Perseus_{}'.format(str(uuid.uuid4()))
        subnet = anat.remote_network(session_id, network, terminal, anchor = anchor)
    if subnet is None:
        subnet = pd.DataFrame({'s': [], 't': [], 'Column Name': []})
        G = nx.Graph()
    else:
        G = nx.from_pandas_edgelist(subnet, 's', 't', edge_attr=True)
        subnet['Column Name'] = data_column
    G.graph['Name'] = data_column
    for node in G:
        if str(node) in set(terminal.astype(str)):
            node_type = 'terminal'
        elif str(node) == str(anchor):
            node_type = 'anchor'
        else:
            node_type = 'connector'
        G.node[node]['Type'] = node_type
    print('Done with', data_column, flush=True)
    return score, subnet, G, terminal.to_frame().assign(**{'Column Name': data_column})

if __name__ == '__main__':
    import perseuspy
    print('perseuspy verion', perseuspy.__version__)
    import argparse
    parser = argparse.ArgumentParser(description='PHOTON: PHOsphoproteomic dissecTiOn using Networks')
    parser.add_argument('paramfile', type=argparse.FileType('r'))
    parser.add_argument('infolder', type=str)
    parser.add_argument('outfolder', type=str)
    parser.add_argument('subnetworks', type=str)
    parser.add_argument('signaling_scores', type=argparse.FileType('w'))
    parser.add_argument('--cpu', type=int, default=max(os.cpu_count() - 1, 1))
    parser.add_argument('--no-parallel', dest='parallel', action='store_false', help='Disable parallel processing. Helpful for debugging.')
    parser.set_defaults(parallel=True)
    args = parser.parse_args()
    print('reading parameters', flush=True)
    paramFile = params.parse_parameters(args.paramfile)
    parameters = {
            'activity' : {
                'min_size' : params.intParam(paramFile, 'Required number of observations'),
                'permutations' : params.intParam(paramFile, 'Number of permutations'),
                'side' : params.singleChoiceParam(paramFile, 'Side')
                }}
    data_columns = params.multiChoiceParam(paramFile, 'Data columns')
    if len(data_columns) < 1:
        print("Please choose at least one data column")
        sys.exit(1)
    confidence_column = params.singleChoiceParam(paramFile, 'Confidence column')
    if type(confidence_column) is int and confidence_column < 0:
        print("Confidence column was not chosen")
        sys.exit(1)
    additional_columns = params.boolParam(paramFile, 'Additional columns')
    run_anat, run_anat_subparam = params.boolWithSubParams(paramFile, 'Reconstruct signaling networks with ANAT')
    top_n_terminals = params.intParam(paramFile, 'Restrict terminals to n top-scoring proteins') if run_anat else 0
    anchor = params.stringParam(run_anat_subparam, 'Signaling source') if run_anat else None
    networks_table, networks = read_networks(args.infolder)
    for guid in networks_table['GUID']:
        name, node_table, edge_table = [networks[guid][key] for key in ['name', 'node_table', 'edge_table']]
        if anchor is not None and anchor not in set(node_table['Node']):
            print("Anchor {} is not contained in network {}".format(anchor, name))
            sys.exit(1)
        if (args.parallel):
            results = Parallel(n_jobs=args.cpu)(delayed(run)(data_column, confidence_column, name, node_table, edge_table, run_anat, anchor, top_n_terminals, parameters) for data_column in data_columns)
        else:
            results = [run(data_column, confidence_column, name, node_table, edge_table, run_anat, anchor, top_n_terminals, parameters) for data_column in data_columns]
        scores, subnets, graphs, terminals = zip(*results)
        # aggregate scores
        score = aggregate_scores(scores, additional_columns)
        score.to_perseus(args.signaling_scores, main_columns = ['{} Signaling score'.format(x) for x in data_columns])
        # aggregate networks
        subnetworks_table, subnetworks = nx.to_perseus(graphs)
        for sub_guid in subnetworks_table['GUID']:
            sub_node_table, sub_edge_table = [subnetworks[sub_guid][key] for key in ['node_table', 'edge_table']]
            subnetworks[sub_guid]['node_table'] = sub_node_table.merge(node_table, on=['Node'])
            subnetworks[sub_guid]['edge_table'] = sub_edge_table.merge(edge_table, on=['Source', 'Target'])
        write_networks(args.subnetworks, subnetworks_table, subnetworks)
        subnet = pd.concat(subnets)
        # annotate input network
        connector = pd.concat([subnet[['s', 'Column Name']].rename(columns={'s': 'Node'}),
            subnet[['t', 'Column Name']].rename(columns={'t': 'Node'})]).astype(str)
        terminal = pd.concat(terminals).rename(columns={'GeneID': 'Node'})
        if len(terminal) > 0:
            terminal['Function'] = 'Terminal'
        else:
            terminal = pd.DataFrame({'Node':[], 'Function':[], 'Column Name':[]})
        if len(connector) > 0:
            connector['Function'] = 'Connector'
        else:
            connector = pd.DataFrame({'Node':[], 'Function':[], 'Column Name':[]})
        _functions = pd.concat([terminal, connector], sort=True)
        if anchor is not None:
            _functions = _functions.append(pd.DataFrame({'Node':anchor, 'Column Name':data_columns, 'Function':'Anchor'}))
        if len(_functions) > 0:
            functions = (_functions.groupby(['Node', 'Column Name'])['Function']
                    .unique().str.join(';').unstack().apply(lambda col: col.astype('category'))
                    .rename(columns = lambda col: '{} Function'.format(col)).reset_index())
            networks[guid]['node_table'] = node_table.merge(functions, how='left')
        networks[guid]['node_table'] = node_table.merge(score, how='left')
        if len(subnet) > 0:
            edges = (pd.concat([subnet, subnet.rename({'s':'t', 't':'s'})])
                    .groupby(['s', 't'])['Column Name']
                    .unique().str.join(';').astype('category').reset_index()
                    .rename(columns={'s' : 'Source', 't': 'Target', 'Column Name': 'Reconstructed networks'}))
        else:
            edges = pd.DataFrame({'Source': [], 'Target': [], 'Reconstructed networks': []})
        networks[guid]['edge_table'] = edge_table.merge(edges, how='left')
    print('writing networks', flush=True)
    write_networks(args.outfolder, networks_table, networks)
