.. include:: ../header.rst

.. _AiDocumentCollection:

=======================
AiDocumentCollection
=======================

The result of :meth:`Utils.LoadAiAsync`: PDFs that have been chunked, embedded, and indexed for AI / RAG.

Use :meth:`AiDocumentCollection.AskAsync` to answer a question from retrieved excerpts, :meth:`AiDocumentCollection.SummarizeAsync` to summarize one source file, and :meth:`AiDocumentCollection.SearchAsync` to rank chunks without calling a chat model.


================================================ =======================================================
**Method / Attribute**                           **Short Description**
================================================ =======================================================
:meth:`AiDocumentCollection.AskAsync`            Answer a question from retrieved excerpts
:meth:`AiDocumentCollection.SummarizeAsync`      Summarize one loaded document
:meth:`AiDocumentCollection.SearchAsync`         Rank chunks by query (no chat)
:meth:`AiDocumentCollection.AddDocumentsAsync`   Index more PDFs
:meth:`AiDocumentCollection.RemoveDocumentAsync` Remove one source PDF
:attr:`AiDocumentCollection.ChunkCount`          Number of indexed chunks
:attr:`AiDocumentCollection.SourceFiles`         Source file names that were indexed
================================================ =======================================================


**Class API**

.. class:: AiDocumentCollection

  .. method:: AskAsync(string question, int? topK: null, string? sourceFileName: null, CancellationToken cancellationToken: default, bool allContext: false)

     Answer a question from retrieved excerpts. Returns the answer text.

     :arg string question: the question.
     :arg int? topK: optional cap on how many excerpts to send.
     :arg string? sourceFileName: optional filter to one source file.
     :arg CancellationToken cancellationToken: optional cancellation.
     :arg bool allContext: send every indexed chunk instead of the top excerpts.

     :rtype: Task<string>
     :returns: the answer text.

  .. method:: SummarizeAsync(string filePathOrName, CancellationToken cancellationToken: default, int? pageNumber: null)

     Summarize one loaded document. Long documents are summarized in batches, then combined.

     :arg string filePathOrName: a source path or file name previously indexed.
     :arg CancellationToken cancellationToken: optional cancellation.
     :arg int? pageNumber: when set (1-based), summarize that page only.

     :rtype: Task<string>
     :returns: the summary text.

  .. method:: SearchAsync(string query, int? topK: null, string? sourceFileName: null, CancellationToken cancellationToken: default)

     Return the best-matching chunks. Does not call a chat model.

     :arg string query: the search query.
     :arg int? topK: optional number of hits to return.
     :arg string? sourceFileName: optional filter to one source file.
     :arg CancellationToken cancellationToken: optional cancellation.

     :rtype: Task<IReadOnlyList<SearchResult>>
     :returns: ranked chunks.

  .. method:: AddDocumentsAsync(IEnumerable<string> pdfPaths, CancellationToken cancellationToken: default)

     Extract, embed, and upsert more PDFs, replacing any previous chunks from the same files.

  .. method:: RemoveDocumentAsync(string filePathOrName, CancellationToken cancellationToken: default)

     Remove one source PDF from the collection and the vector index.

  .. attribute:: ChunkCount

     Number of indexed chunks across all loaded PDFs.

     :type: int

  .. attribute:: SourceFiles

     Source file names that were indexed.

     :type: IReadOnlyList<string>


.. include:: ../footer.rst
