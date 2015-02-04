# Primitive (c) TextIndexing library

This library provides the way to index text documents by words they contain.

## Usage

First you should create the `Indexer` instance

````C#
var indexer = Indexer.Create();
````

An `IndexerCreationOptions` instance with the following additional options can be specified as an argument to
this method:
 
- string comparison type used to compare words in index
- `IStreamParser` or `ILineParser` that defines the way words would be extracted from documents content

Then you can add one or several document sources to obtain documents
from. There are two standard implementations of document source:
`SingleFileDocumentSource` and `DirectoryDocumentSource`.

`````C#
indexer.AddSource(new DirectoryDocumentSource(baseDirectory, "*.cs")); 
indexer.AddSource(new SingleFileDocumentSource(Path.Combine(baseDirectory, "example.txt"));
`````

The `Indexer` provides the `Index` property which can be used then to query
documents from the index:

`````C#
// matches only "apple" word, returns single WordDocuments collection
var appleDocuments = indexer.Index.GetExactWord("apple");

// matches all words starting with "ban" and returns the list of WordDocuments
// for each word matched
var bananaDocuments = indexer.Index.GetWordsStartWith("ban");
`````
