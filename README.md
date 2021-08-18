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

## PluginPyMOL
Description

### Contacts
Developer: Bryan

Supervisor: Christoph and Sule

Maintenance: Juergen and Daniela

## PluginXVis
Description

### Contacts
Developer: Bryan

Supervisor: Christoph and Sule

Maintenance: Juergen and Daniela

