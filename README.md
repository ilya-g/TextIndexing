# Primitive (c) TextIndexing library

This library provides the way to index text documents by words they contain.

## Basic usage

First you should create the `IndexerSet` instance

````C#
var indexerSet = IndexerSet.Create();
````

An `IndexerCreationOptions` instance with the following additional options can be specified as an argument to
this method:
 
- string comparison type used to compare words in index
- `IStreamParser` or `ILineParser` that defines the way words would be extracted from documents content

Then you can add one or more document sources to obtain documents
from. There are two standard implementations of document source:
`SingleFileDocumentSource` and `DirectoryDocumentSource`.

`````C#
indexerSet.Add(new DirectoryDocumentSource(baseDirectory, "*.cs")); 
indexerSet.Add(new SingleFileDocumentSource(Path.Combine(baseDirectory, "example.txt"));
`````

The `IndexerSet` provides the `Index` property which can be used then to query
documents from the index:

`````C#
// matches only "apple" word, returns single WordDocuments collection
var appleDocuments = indexerSet.Index.GetExactWord("apple");
`````

The returned instance of `WordDocuments` is a collection of `DocumentInfo`s, each pointing to the original document containing the word being searched for.

`````C#
// matches all words starting with "ban" and returns the list of WordDocuments
// for each word matched
var banWords = indexerSet.Index.GetWordsStartWith("ban");
`````

This query will return sequence of `WordDocuments`, one for each matching word. Then you can flatten this sequence with SelectMany operation:
`var banDocuments = banWords.SelectMany(wordDocuments => wordDocuments);`

## Advanced usage

An `IIndex` instance can be used without creating `IndexerSet`. The following example shows, how to create an index and attach an Indexer to it:

`````C#
var options = new IndexerCreationOptions();
var index = options.CreateIndex();
var parser = options.GetDefaultStreamParser();

var indexer = new Indexer(
    index, 
    new DirectoryDocumentSource(AppDomain.CurrentDomain.BaseDirectory, "*.txt"), 
    parser);
indexer.StartIndexing();
`````

## Custom document parsers

To create custom document parser one should implement `ILineParser` or `IStreamParser` interface. The former being provided with the text line by line should extract all words it can from each individual line. No state should be shared between calls by `ILineParser` implementation. The latter is provided with `TextReader` and can read entire document with it.
