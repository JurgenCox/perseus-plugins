import sys
import os
import re
import __main__

if (len(sys.argv) != 7) and False:
    raise Exception ('There was an error parsing your inputs. Please make sure to fill out the necessary fields.')
try:
    has_pymol = True
    import pymol
    from pymol import cmd, util
except ModuleNotFoundError:
    has_pymol = False
if not has_pymol:
    raise Exception(('Unable to import pymol. Make sure to select the python.exe inside your pymol folder.'
                    'This is NOT the same as your native python runtime!'))
try:
    import numpy as np
    from perseuspy import pd
    from Bio import pairwise2, SeqIO
    from Bio.PDB.PDBParser import PDBParser
    from Bio.PDB.Polypeptide import PPBuilder
    has_everything_else = True
except ModuleNotFoundError:
    has_everything_else = False
if not has_everything_else:
    raise Exception('Necessary packages not found. Please run first time setup to install packages!')



def get_pdb_seq(pdbfile):
    structure = PDBParser().get_structure('myID', pdbfile)
    ppb = PPBuilder()
    return [str(pp.get_sequence()) for pp in ppb.build_peptides(structure)]

def get_maxlynx_seq(fasta, protein, regex):
    """
    Searches for the fasta file records to see if any of them match the fasta file IDs.
    """
    fasta_file = SeqIO.parse(fasta, "fasta")
    if regex[0] == '>':
        regex = regex[1:]
    for record in fasta_file:
        match = re.match(regex, record.id)
        if not match:
            continue
        match = match.group(1)
        if match == protein:
            return str(record.seq)
    raise Exception(f'No sequence matching your specified Protein identifier was found in your fasta file. Check your spelling and try again.')

_, pdb_filepath, fasta_filepath, protein_of_interest, identifier_regex, infile, outfile = sys.argv # read arguments from the command line


__main__.pymol_argv = ['pymol', pdb_filepath]

pdb_sequence = get_pdb_seq(pdb_filepath)[0] #TODO need to figure out which one it is...

maxlynx_sequence = get_maxlynx_seq(fasta_filepath, protein_of_interest, identifier_regex)


alignments = pairwise2.align.globalxx(pdb_sequence, maxlynx_sequence, one_alignment_only=True)
alignment = alignments[0]
pdb_aligned_seq = alignment.seqA
maxlynx_aligned_seq = alignment.seqB
pdb_start = re.search(r'[^-]', pdb_aligned_seq).span()[0]
maxlynx_start = re.search(r'[^-]', maxlynx_aligned_seq).span()[0]

diff = maxlynx_start - pdb_start #can then add diff to maxlynx numbers to 

df = pd.read_perseus(infile) # read the input matrix into a pandas.DataFrame
out_df = df.copy()

# Remove non-real non-crossliks TODO: mono/looplink support?? 
df = df[(df['Decoy'] == 'forward') & (df['Crosslink Product Type'].str.contains('ProXL'))].copy()
# Adjust to pdb positions from maxlynx positions
positions = ['Pro_InterLink1', 'Pro_InterLink2']
for pos_header in positions:
    df.loc[:, pos_header] = df.loc[:, pos_header].replace(r'[\D]', '', regex=True).astype(int) + diff
df = df[(df['Pro_InterLink1'] >= 0) & (df['Pro_InterLink2'] >= 0)] #filter crosslinks that are not in the pdb
df = df.drop_duplicates(['Proteins1', 'Proteins2', 'InterLinks'])
pymol.finish_launching()
cmd.load(pdb_filepath)
cmd.set('cartoon_transparency', 0.7)
util.cbc()
cmd.remove('resn hoh')
out_df['Crosslink Distance'] = np.nan
c_alphas = set()
for row in df.itertuples():
    point_a = row.Pro_InterLink1
    point_b = row.Pro_InterLink2
    ind = row.Index
    out_ind = (out_df['Proteins1'] == row.Proteins1) & (out_df['Proteins2'] == row.Proteins2) & (out_df['InterLinks'] == row.InterLinks)
    # TODO use: ???/{point_a}/CA, find which 
    dist = cmd.distance(f'dist{ind}', f'A/{point_a}/CA', f'A/{point_b}/CA')
    c_alphas.add(point_a)
    c_alphas.add(point_b)
    out_df.loc[out_ind, 'Crosslink Distance'] = dist or -1

ca_selector = ' or '.join(f'(resi {point})' for point in c_alphas)
cmd.select(f'Crosslinked_CAlphas', f'(chain A) and (n. CA) and ({ca_selector})')

out_df.to_perseus(outfile)
