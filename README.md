perseus-plugins
===============

[![NuGet version](https://badge.fury.io/nu/PerseusApi.svg)](https://www.nuget.org/profiles/coxgroup)

Perseus is a software framework for the statistical analysis of omics data. It has a plugin architecture which allows users to contribute their own data analysis activities. Here you find the code necessary to develop your own plugins.

License:
please see the LICENSE file in this folder. 

## Getting Started

### Prerequisites
<b>for usage</b>
<ul>
<li>.NET Standard 2.0</li>
</ul>

<b>for developments</b>
<ul>
<li>Visual Studio</li>
</ul>


# Plugins avaliable
## PluginInterop

[![NuGet version](https://badge.fury.io/nu/PluginInterop.svg)](https://www.nuget.org/packages/PluginInterop)

This repository contains the source code for `PluginInterop`, the Perseus plugin that provides the foundation for plugins developed in e.g. `R` and `Python`, and allows them to be executed from within the Perseus workflow.

The plugin is designed to work with other Perseus interop efforts such as:

 * [PerseusR](https://www.github.com/cox-labs/PerseusR) for developing plugins in `R`.
 * [perseuspy](https://www.github.com/cox-labs/perseuspy) for developing plugins in `Python`.

Developer: Dr. Jan Rudolph [@Github page](https://github.com/jdrudolph)
Maintenance: Daniela Ferretti - [@GitHub page](https://github.com/danielaferretti1992)

info: [here](https://github.com/cox-labs/PluginInterop)

## PluginPHOTON
Phosphoproteomic experiments typically identify sites within a protein that are differentially phosphorylated between two or more cell states. However, the interpretation of these data is hampered by the lack of methods that can translate site-specific information into global maps of active proteins and signaling networks, especially as the phosphoproteome is often undersampled. Here, we describe PHOTON, a method for interpreting phosphorylation data within their signaling context, as captured by protein-protein interaction networks, to identify active proteins and pathways and pinpoint functional phosphosites. We apply PHOTON to interpret existing and novel phosphoproteomic datasets related to epidermal growth factor and insulin responses. PHOTON substantially outperforms the widely used cutoff approach, providing highly reproducible predictions that are more in line with current biological knowledge. Altogether, PHOTON overcomes the fundamental challenge of delineating signaling pathways from large-scale phosphoproteomic data, thereby enabling translation of environmental cues to downstream cellular responses.
[@Pubmed link](https://pubmed.ncbi.nlm.nih.gov/28009266/)

[Pubmed link](https://www.ncbi.nlm.nih.gov/pubmed/28009266)

### Installation and Tutorial

Follow the [installation instructions](Setup/docs/installation.md) and
[learn](Setup/docs/tutorial.md) how to use PHOTON from within Perseus.

Developer: Dr. Jan Rudolph [@Github page](https://github.com/jdrudolph)
Maintenance: Daniela Ferretti - [@GitHub page](https://github.com/danielaferretti1992)

info: [here](https://github.com/jdrudolph/photon)
## PluginMetis
PluginMetis: a new plugin for the [Perseus](https://www.maxquant.org/perseus/) software aimed at analyzing quantitative multi-omics data based on metabolic pathways. Data from different omics types are connected through reactions of a genome-scale metabolic pathway reconstruction. Metabolite concentrations connect through the reactants, while transcript, protein and protein post-translational modification (PTM) data are associated through the enzymes catalyzing the reactions. Supported experimental designs include static comparative studies and time series data. 

Developer: Dr. Hamid Hamzeiy - [@GitHub page](https://github.com/hamidhamzeiy)
Daniela Ferretti - [@GitHub page](https://github.com/danielaferretti1992)

## PluginDependentPeptides
Perseus plugin for importing dependent peptide search results
from [MaxQuant](https://www.biochem.mpg.de/5111795/maxquant).
Requires [PluginInterop](https://github.com/cox-labs/PluginInterop)
and [Python](https://www.python.org/) with
[perseuspy](https://www.github.com/cox-labs/perseuspy) installed.

Developer: Dr. Jan Rudolph [@Github page](https://github.com/jdrudolph)

info: [here](https://github.com/cox-labs/PluginDependentPeptides)

## PluginProteomicRuler
This Perseus plugin implements computational strategies for absolute protein quantification without spike-in standards. The concept was published by [Wiśniewski, Hein, et al., MCP, 2014](https://pubmed.ncbi.nlm.nih.gov/25225357/) and builds on earlier work by [Wiśniewski et al., MSB, 2012](https://pubmed.ncbi.nlm.nih.gov/22968445/). 

Developer: 

info: [here](http://www.coxdocs.org/doku.php?id=perseus:user:plugins:proteomicruler)

## PluginsCrosslinks

### Measure Cross Linking Euclidean distances by using [PyMol](https://pymol.org/2/)
Visualize your intra-protein cross links from the MaxLynx results in 3D using the PyMOL software and measure Euclidean distances between two alpha carbons of linked residues after a sequence alignment.
#### Prerequisites:
Output from Maxquant/MaxLynx _only_.

PyMol (Download at https://pymol.org/2/). Remember where you installed this!

single-chain, 3D protein structure PDB File (Multiple Chains will be ignored)

FASTA File containing the protein sequence

Admin Rights (for dependency installation)

#### Instructions
1. Load the crosslinkMsms.txt table from MaxLynx/MaxQuant. Include all default columns plus "InterLinks" and "Crosslink Product Type" (as "text" column type)
2. Enter the PDB file, and the fasta file which includes the corresponding protein
3. Use the protein identifier and protein identifier type boxes to specify the protein being visualized
4. Go to the PyMol installation directory and select the python.exe there. For single user installations in windows, this is "C:\Users\[name]\AppData\Local\Schrodinger\PyMOL2" by default.
5. Check the "first time setup" box if this is your first time running the plugin (or if pymol was reinstalled)

### Generate Input for [XVis](https://xvis.genzentrum.lmu.de/) Cross link Visualizer or [XLinkAnalyzer](https://www.embl-hamburg.de/XlinkAnalyzer/XlinkAnalyzer.html)(Chimera cross link plugin)
#### Instructions
1. Load any perseus matrix which contains at minimum the following 5 columns for: identifiers for protein 1 and 2, confidence score,
and the absolute positions of the two crosslinked amino acids within the protein sequences.
2. Open the plugin in Cross link -> XVis/XLinkAnalyzer input, and enter the names of the columns above.
3. Optionally add more information if you are using this for XLinkAnalyzer, such as peptide IDs and relative linking positions
4. Specify an output directory and filename. Do not include the extension (.csv).
5. Click OK. No new matrix will be generated and instead a .csv will be written to the specified location, which can then 
be used as input to the corresponding visualization programs.


### Contacts
Developer: Bryan [@Github page](https://github.com/BryanZWu)

