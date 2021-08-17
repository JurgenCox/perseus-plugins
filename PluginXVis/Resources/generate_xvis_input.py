from perseuspy import pd
import os
from pysam import FastaFile

_, overwrite, fastafile, destinationdir, infile, outfile = sys.argv # read arguments from the command line

def find_protein_len(fasta, out='xvis_protein_len.csv'):
    df=pd.DataFrame({'Protein':[], 'Length':[]})
    df = df.set_index('Protein')
    sequences_object = FastaFile(fasta)
    for protein in sequences_object.references:
        df.loc[protein] = sequences_object.get_reference_length(protein)
    df.to_csv(out)
    return df


def generate_xvis_csv(exp_data, name='xinet-crosslink.csv'):
    """
    Generates a csv competible with the xvis input format (http://crosslinkviewer.org/CLMS-CSV.php)
    and writes it to the file specified by name, in the out directory. Will 
    throw an error if something already exists. 
    """
    if not overwrite:
        assert not os.path.isfile(os.path.join(destinationdir, name)), 'Destination file already exists!'
    df = df[(df['Decoy'] == 'forward') & (df['Crosslink Product Type'].str.contains('ProXL'))].copy()
    df.loc[:, 'Crosslink Sequence'] = df['Sequence1'].astype(str) + '--' + df['Sequence2'].astype(str)
    mappings = { #newcol: oldcol
        'Protein1': 'Proteins1',
        'AbsPos1': 'Pro_InterLink1',
        'Protein2': 'Proteins2',
        'AbsPos2': 'Pro_InterLink2',
    }
    out_csv = pd.DataFrame({newcol: data[oldcol] for newcol, oldcol in mappings.items()})
    positions = ['AbsPos2', 'AbsPos1']
    for pos_header in positions:
        out_csv.loc[:, pos_header] = out_csv.loc[:, pos_header].replace(r'[\D]', '', regex=True)
    out_csv = out_csv.drop_duplicates(ignore_index=True)
    out_csv.to_csv(os.path.join(destinationdir, name))
    return out_csv