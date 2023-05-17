# Integrated network analysis of EGF and IGF stimulation using PHOTON

Make sure to follow the [installation instructions](/docs/Perseus/installation.md).
Please create an [issue](https://github.com/jdrudolph/photon/issues) if the
instructions are not clear, or if you run into any problems.

The goal of our analysis is to 1) derive signaling functionality scores for the
proteins in our network and 2) reconstruct the underlying signaling pathway. In
brief, signaling functionality scores describe whether a protein promotes (high
positive score) or demotes (high negative score) the phosphorylation of its
interactors. We can utilize the proteins with significant scores to extract a
signaling pathway from the PPI network that connects the functional proteins.

# Data preparation
## Phosphorylation data
The standard analysis of PTM data is not covered in this tutorial. Only the
most elementary steps are listed here, skipping e.g. data exploration and
normalization. The dataset we are going to work with consists of stimulated
MCF7 cells measured in duplicates. The samples were measured with SILAC. All
reported phosphorylation fold-changes correspond to stimulated/unstimulated
condition. 

1. Download the data
   [here](/docs/Perseus/phosphoSTY_rudolph.txt).

1. Load the file `phosphoSTY_rudolph.txt` (**Matrix => Load => Generic matrix upload**).

2. We will eventually map this data to the STRING PPI network. Therefore we need to make sure
that the network and the data are defined in terms of the same protein identifiers. Here we
choose to covert the UniProt ids in the data to the ensemble protein ids (ENSP) used in
the network (**Matrix => Processin => Annot. columns => Add annotation**). Select
`mainAnnot.homo_sapiens.txt.gz` and annotate with `ENSP`.

## Obtaining a high-confidence mouse protein-protein interaction network from STRING
PPI networks are usually obtained from databases. A popular choice is STRING, which we will be using here.

1. Download the `9506.protein.links.v10.5.txt.gz` file from
   [string-db](https://string-db.org/cgi/download.pl?species_text=Homo+sapiens).
   It contains all physical interactions between proteins in human.

1. We start by loading the STRING network table (**Matrix => Load => Raw upload**).
   Choose file `9606.protein.links.v10.5.txt.gz` and separating columns by 'space'.

2. The technologies used to generate large-scale interaction networks are not
   perfect and therefore interaction databases often contain a fraction of
   false interactions. STRING provides a confidence score, which we can use to
   filter out low-confidence interactions. First we have to change to column
   type of the score column (**Matrix => Rearrange => Change column types**).
   Convert `comined_score` from 'Text' to 'Numeric'.

3. We can now filter the edges and observe the changes in number of edges and
   score distribution by looking at the histogram of the scores before and
   after filtering  (**Matrix => Filter rows => Filter rows based on
   numerical/main column**). Set `x >= 900` and create the histogram (**Matrix
   => Visualization => Histogram**). Select `combined_score` and set the y-axis
   to log-scale.

4. To make it clearer that the `combined_score` represents the confidence in
   each interaction, we will scale its values to the commonly used range
   between 0 and 1 (**Matrix => Basic => Transform**). Select `combined_score`
   and `x / 1000` and then rename the column (**Matrix => Rearrange => Rename
   columns**). Change `combined_score` to `Confidence`.

5. In order to improve compatability of the network with UniProt annotations we
   will change the format of the protein identifiers and remove the species
   prefix (**Matrix => Rearrange => Process text column**). Select both columns
   and set 'Regular expression' to `^9606\.(.*)`. Make sure not to add any extra
   symbols or spaces to the regular expression.

6. The resulting edge table provides the template for our network. We will save
   this intermediate result to file for use in the next step  (**Matrix =>
   Export => Generic matrix export**). Save as
   `9606.protein.links.v10.5_high_confidence.txt`.

## Create the PPI network from the edge table and prepare it for data annotation

1. If necessary, load the edge table from the previous step (**Matrix => Load
   => Generic matrix upload**). Choose file `9606.protein.links.v10.5_high_confidence.txt`.

2. Next to the ‘Matrix' tab Perseus has a 'Network' tab that lists all
   operations that can be performed on networks. Now we can transform the
   matrix into the dedicated ‘Network collection’ data structure (**Network →
   From matrix → Basic → From matrix**). Choose `protein1` and `protein2` as
   'Source' and 'Target', and check the `Network is directed` option.

3. Take your time to browse and understand the different tables that represent
   a 'Network Collection'. Whenever you select a network collection in the
   workflow, tables in separate tabs will give you different levels of
   information on the networks in the collection. Here we have only a single
   network, which is represented by a single line in the `Graphs` tab. Next to
   the `Graphs` tab you can select the tab of the network, named accordingly.
   Each network is itself represented by two tables. The 'Nodes' table provides
   details on all the molecules that are part of the interaction network and
   will always contain a `Node` column that identifies each entry. The 'Edges'
   table describes all interactions between the nodes and always contains at
   least a `Source` and `Target` column. Keep coming back to these tables in
   order to see the effect of any of the next processing steps.

4. We can explore the topology of the network by calculating node degrees
   (number of neighbors of any given node, **Network => Topology => Node
   degrees**).

5. The node degrees were added to the 'Node' table of the network. Make sure
   you can find them. It is easiest to plot them using any of the matrix
   visualizations in Perseus. We can easily convert network tables to matrices
   (**Network => To matrix => Basic => To matrix**). Select 'Nodes'.

6. When we plot the histogram of node distributions we can see that there are a
   number nodes with very large degrees (**Matrix => Analysis => Visualization
   => Histogram**). Select `Degree` and change the y-axis to log-scale.

7. Finding out the identity of the proteins by browsing ensemble protein
   identifiers is tedious.  Instead, we would like to map them to gene names.
   We can't do so directy, but first have to map ENSP to UniProt identifiers
   (**Matrix => Processing => Annot. columns => To base identifiers**). Select
   `mainAnnot.homo_sapiens.txt.gz` and 'Identifier type' as `ENSP`. Now we can
   add annotations as usual (**Matrix => Processing => Annot. columns => Add
   annotation**). Select `mainAnnot.homo_sapiens.txt.gz`, set 'UniProt column'
   to `UniProt` and add the `Gene name` and `GOBP name` annotation.

8. Sorting by degree will show you the highest connected proteins in the
   network. Due to their high connectivity, These proteins can have a
   dominating effect on network analyses. Therefore, unless these proteins are
   of interest, removing them from the network collection can be helpful
   (**Network => Processing => Filter nodes => Filter nodes by numerical
   column**). Enter `x < 1000`.

9. [Optional] This preprocessed intermediate result can be saved to the hard
   drive as a folder (Network → Export → Generic network export). Create a
   folder named `9606.protein.links.v10.5_preprocessed` at your desired
   location and select it as save destination. Use the file explorer to
   navigate to the save location and open the network folder. You will see a
   series of `.txt` files that represent the network tables you were browsing
   in Perseus. You can inspect and edit these tables with any decent text
   editor (Notepad might not be able to handle the files due to size). Due to
   the simple tab-separated format, it is easy to import the network into other
   tools, such as Cytoscape.

## Perform PHOTON analysis and visualize the results
1. We are finally ready to perform the analysis. Make sure that the data
   preparation was successful and load intermediate result from file if
   necessary.

2. To annotate the network with the experimental data, first select the network
   collection in the workflow, then, while holding the CTRL key, select the
   data table. With both workflow items selected, perform the annotation
   (**Network → Merge with matrix → Annotate → Annotate nodes**). Match `Node` with
   `ENSP` and copy all main columns. In order to not collapse the peptide-level
   information of the data to the protein-level network, we set 'Combine copied
   main values' to `keep separate`.

3. Make sure that the node table of the network contains multi-numeric columns
   with the data.

4. Perform PHOTON analysis to obtain 'Signaling functionality scores' for all
   proteins in the network.  (**Network => Processing => Modifications =>
   PHOTON**). Select the `Reconstruct signaling networks using ANAT` option and
   enter `ENSP00000265171` (ENSP id of EGF stimulus) as signaling source.

5. For convenience the result is listed in different data nodes. The big
   network collection contains all results of the analysis. The second network
   collection contains reconstructed signaling networks. The matrix contains all
   signaling functionality scores which can be analyzed further, e.g. by
   clustering (**Matrix => Processing => Annot. columns => To base
   identifiers**). Select `mainAnnot.homo_sapiens.txt.gz` and 'Identifier type'
   as `ENSP` (**Matrix => Processing => Annot. columns => Add annotation**).
   Select `mainAnnot.homo_sapiens.txt.gz`, set 'UniProt column' to `UniProt`
   and add the `Gene name` and `GOBP name` annotation (**Matrix => Analysis =>
   Hierarchical clustering**). The time points will cluster together. You can
   flip branches of the cluster dendrogram by CTRL+click on the branching
   points. Can you identify some interesting cluster.

6. You can create a basic visualization of the reconstructed signaling networks
   directly inside Perseus. First add gene name annotations
   to the network (Network → Processing → Annot. columns → To base
   identifiers). Select `mainAnnot.homo_sapiens.txt.gz` and ‘Identifier type’
   as `ENSP`. Add name and GO annotations (Network → Processing → Annot.
   columns → Add annotation). Select `mainAnnot.homo_sapiens.txt.gz`, set
   ‘UniProt column’ to `UniProt` and add the `Gene name` annotation. Visualize
   the network as a node-link diagram (Network → Analysis → Visualization →
   Kinase-substrate network). Select `Gene name` as ‘Node column’ and `From
   network name` as ‘Data column’.

This covers the basic analysis. The kinase functionality scores can be analyzed in a similar way
to protein quantification, i.e. via clustering, statistical testing etc.

