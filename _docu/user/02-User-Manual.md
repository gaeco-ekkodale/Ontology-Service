# Introduction

This document describes the Ontology Service from a user's point of view: what an ontology
decides in gaeco, what the file looks like, and what happens when it is replaced.

The Ontology Service has no user interface of its own. Everything a user does with an
ontology is done through the **Platform Config** module in the Plugin Host, covered in the
[Platform Config user manual](https://github.com/gaeco-ekkodale/PlatformConfig). This
document explains what sits behind that module.

# Prerequisites

- The `Ontology Server` and `Ontology Postgres` must be running.
- `Kafka` and `MiniO` must be running — the ontology is stored as a file and published to
  the other services as an event.
- The `PluginHost Service` and the `Platform Config` client must be available to upload an
  ontology through the user interface.

# What an Ontology Decides

The guideline says *what exists*: the classifications and their properties. The ontology says
*how those things may be connected*.

It declares the permitted relationship types between classifications — that a portfolio has
buildings, that a building has floors, that a floor has spaces. When you draw a connection
between two instances in the Instances module, the dialog that appears offers exactly the
relationships the ontology permits between their two classifications. Nothing else can be
created.

That is the practical consequence worth remembering: **a relationship missing from the
ontology cannot be created in the platform at all.** If two instances refuse to connect, the
ontology is the first place to look — not the access rights, and not the guideline.

The guideline and the ontology are uploaded separately and both are required. Together they
are the first of the three setup steps the start page asks for.

# The Ontology File

An ontology is a single file in **Turtle** format, extension `.ttl`. It is ordinary RDF, so
any RDF tooling can read and validate it.

The structure has two parts. Classes, one per classification:

```turtle
@prefix rdfs: <http://www.w3.org/2000/01/rdf-schema#> .
@prefix ibpdi: <https://ibpdi.org/ontology/2.0/> .

ibpdi:Building a rdfs:Class ;
    rdfs:label "Building" ;
    rdfs:comment "IBPDI Real Estate CDM class Building." .
```

And properties that connect them, each with a domain and a range:

```turtle
ibpdi:buildingHasFloor a rdf:Property ;
    rdfs:label "Building has Floor" ;
    rdfs:domain ibpdi:Building ;
    rdfs:range  ibpdi:Floor .
```

`rdfs:domain` is the classification a relationship starts from, `rdfs:range` the one it
points to. The label is what the Instances module shows in the relationship dialog, so it is
worth writing it as a readable phrase rather than a camel-case identifier.

The class names must line up with the classifications in the guideline. A class the guideline
does not know contributes nothing, and a relationship whose domain or range is unknown can
never be offered.

A ready-made example ships with the deployment repository at
`gaeco-ext/demodata/IBPDI/IBPDI.ttl` — the relationship half of the IBPDI Real Estate Common
Data Model. Like the guideline, it is exported from the **Guideline Editor** rather than
written by hand.

# Uploading an Ontology

Uploads go through Platform Config: open the **Ontology** tab and choose **+**. The file must
be a `.ttl`. See
[Platform Config](https://github.com/gaeco-ekkodale/PlatformConfig)
for the walkthrough.

As with the guideline, the upload is published as an event and the other services build their
own view of it. The ontology is much smaller than a guideline, so this is quick — but it is
still not instant, and a relationship dialog that offers nothing immediately after an upload
is usually just early.

# Replacing and Removing

Each row offers **Replace file**, **Download** and **Delete**.

Replacing narrows or widens what can be connected in the future. It does **not** retroactively
delete relationships that already exist: an instance graph can therefore end up holding a
relationship that the current ontology would no longer permit. That is not corruption, but it
does mean an ontology change is not a way to clean up existing data — the relationships have
to be removed in the Instances module.

Adding relationship types is safe. Removing them is what to be careful about, for the reason
above.

# Where to Look When a Connection Is Refused

In order of likelihood:

1. **The ontology does not permit it.** Download the file and check whether a property exists
   with the right `rdfs:domain` and `rdfs:range` for the two classifications involved. Note
   that relationships are directional — `Building → Floor` does not imply `Floor → Building`.
2. **The upload has not propagated yet.** Reload and try again.
3. **You lack write access to the instance you started from.** The Instances module only
   begins a connection from an instance you may change; otherwise the click is ignored and the
   cursor shows as blocked. See the
   [Access Rights manual](https://github.com/gaeco-ekkodale/AccessService).
4. **The class names do not match the guideline.** An ontology that uses a different
   vocabulary than the uploaded guideline declares relationships between classifications the
   platform does not have.

# Developer Documentation

The service architecture, the data model and the event contract are described in the
[developer documentation](../developer/01-Concepts.md). For modelling ontologies there is
also [How_To_Ontology.md](../How_To_Ontology.md) in this repository.
