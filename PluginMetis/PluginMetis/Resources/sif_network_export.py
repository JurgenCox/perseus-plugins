import sys
from perseuspy import pd
from perseuspy.parameters import *
from perseuspy.io.perseus.matrix import *

# read arguments from the command line
_, paramfile, infile, outfile = sys.argv 
# parse the parameters file
parameters = parse_parameters(paramfile) 
# read the input matrix into a pandas.DataFrame
df = pd.read_perseus(infile) 

reactions = singleChoiceParam(parameters, "Reactions")
modifiers = singleChoiceParam(parameters, "Modifiers")
reactants = singleChoiceParam(parameters, "Reactants")
products = singleChoiceParam(parameters, "Products")

reactionsModifiers = df.loc[:, [reactions, modifiers]]
modifiersReactions = df.loc[:, [modifiers, reactions]]
reactantsReactions = df.loc[:, [reactants, reactions]]
reactionsProducts = df.loc[:, [reactions, products]]

reactionsModifiers.loc[:, 'Relationship Type'] = 'rp'
modifiersReactions.loc[:, 'Relationship Type'] = 'pr'
reactantsReactions.loc[:, 'Relationship Type'] = 'cr'
reactionsProducts.loc[:, 'Relationship Type'] = 'rc'

reactionsModifiers.columns = ['Source', 'Target', 'Relationship Type']
modifiersReactions.columns = ['Source', 'Target', 'Relationship Type']
reactantsReactions.columns = ['Source', 'Target', 'Relationship Type']
reactionsProducts.columns = ['Source', 'Target', 'Relationship Type']

sif = pd.concat([reactionsModifiers, modifiersReactions, reactantsReactions, reactionsProducts], ignore_index=True, sort =False)

sif = sif[pd.notnull(sif['Source'])]
sif = sif[pd.notnull(sif['Target'])]

sif = sif[['Source', 'Relationship Type', 'Target']]

# encoding for output to perseus
sif['Source'] = sif['Source'].apply(lambda s: s.encode(encoding='utf_8', errors='replace'))
sif['Relationship Type'] = sif['Relationship Type'].apply(lambda s: s.encode(encoding='utf_8', errors='replace'))
sif['Target'] = sif['Target'].apply(lambda s: s.encode(encoding='utf_8', errors='replace'))

print(sif.info())

# write pandas.DataFrame in Perseus txt format
sif.to_perseus(outfile)
