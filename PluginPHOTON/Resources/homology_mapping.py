from itertools import takewhile
import numpy as np
from perseuspy import pd
import perseuspy.parameters as params

if __name__ == '__main__':
    import argparse
    parser = argparse.ArgumentParser(description='Map geneid from human to mouse using homology information')
    parser.add_argument('paramfile', type=argparse.FileType('r'))
    parser.add_argument('infile', type=argparse.FileType('r'))
    parser.add_argument('outfile', type=argparse.FileType('w'))
    args = parser.parse_args()
    print('reading parameters')
    paramFile = params.parse_parameters(args.paramfile)
    data = pd.read_perseus(args.infile)
    column_names = data.columns.get_level_values('Column Name') 
    data_columns = [column_names[i] for i, t in takewhile(lambda tup: tup[1] is np.dtype('float64'), enumerate(data.dtypes))]
    geneid_column = params.singleChoiceParam(paramFile, 'GeneID')
    if type(geneid_column) is int and geneid_column < 0:
        print("GeneID column was not chosen")
        sys.exit(1)
    mapping_direction = params.singleChoiceParam(paramFile, 'Map from')
    known_mappings = {'human to mouse' : ('9606', '10090', 'mouse'), 'mouse to human': ('10090', '9606', 'human')}
    if mapping_direction not in known_mappings:
        print('Unknown mapping direction')
        sys.exit(1)
    ids = data[geneid_column].fillna('').astype(str)
    flat_ids = (pd.DataFrame([{i: v for i,v in enumerate(x)} for x in ids.str.split(';').tolist()], index=ids.index)
        .stack()
        .reset_index(1, drop=True)
        .reset_index()
        .rename(columns = {0 : geneid_column}))
    flat_ids = flat_ids[flat_ids[geneid_column] != '']
    import urllib.request
    try:
        print('Downloading homology mapping')
        tmpfile, _ = urllib.request.urlretrieve('http://www.informatics.jax.org/downloads/reports/HOM_MouseHumanSequence.rpt')
    except Exception as ex:
        print('Could not download homology file')
        print(ex)
    print('Mapping identifiers')
    hom_rpt = pd.read_table(tmpfile, dtype=str)
    from_id, to_id, species = known_mappings[mapping_direction]
    map_from = hom_rpt[hom_rpt['NCBI Taxon ID'] == from_id][['HomoloGene ID', 'EntrezGene ID']].rename(columns = {'EntrezGene ID' : geneid_column})
    map_to = hom_rpt[hom_rpt['NCBI Taxon ID'] == to_id][['HomoloGene ID', 'EntrezGene ID']]
    mapping = map_from.merge(map_to)

    new_geneid_column = 'GeneID {}'.format(species)
    mapped = (flat_ids
            .merge(mapping)
            .drop(geneid_column, 1)
            .groupby('index')
            .apply(lambda x : pd.Series({new_geneid_column: ';'.join(x['EntrezGene ID']), 'HomoloGene ID': ';'.join(x['HomoloGene ID'])})))
    if isinstance(data.columns, pd.core.index.MultiIndex):
        mapped.columns = pd.MultiIndex.from_tuples([[x] + ['' for _ in data.columns.names[1:]] for x in mapped.columns], names=data.columns.names)
    if 'HomoloGene ID' in data.columns:
        mapped = mapped.rename(columns={'HomoloGene ID': 'HomoloGene ID_'})
    data.join(mapped).to_perseus(args.outfile, main_columns = data_columns)
