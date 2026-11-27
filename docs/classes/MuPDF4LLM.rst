.. include:: ../header.rst

.. _MuPDF4LLM:

================
MuPDF4LLM
================

Layout analysis switch for Markdown, JSON, text extract, and chunks. Same namespace as :class:`Document`: ``using MuPDF.NET;`` is enough.

:meth:`MuPDF4LLM.SetLayout` (``true``) turns the **layout pipeline** on and registers a layout provider when one is available. :meth:`MuPDF4LLM.SetLayout` (``false``) turns it off (classic extract). :meth:`Document.ToMarkdown` works in both modes. :meth:`Document.ToJson`, :meth:`Document.ToText` and :meth:`Document.ToChunks` require the pipeline to be on.

:meth:`Utils.SetLayout` (also :meth:`Utils.UseLayout`) is a wrapper around :meth:`SetLayout`. :attr:`Utils.LayoutEnabled` is the same value as :attr:`LayoutEnabled`.


.. note::

   Layout analysis is optional. It uses a Python worker (Python 3.11+ on ``PATH``, then ``dotnet msbuild -t:MuPDFNetSetupLayoutPython``). When that worker is installed, the pipeline is on by default. Without it, keep :meth:`SetLayout` (``false``) and use classic extract.


================================================ =======================================================
**Method / Attribute**                           **Short Description**
================================================ =======================================================
:meth:`MuPDF4LLM.SetLayout`                      Enable or disable the layout analysis pipeline (also SetUseLayout)
:attr:`MuPDF4LLM.LayoutEnabled`                  Whether the layout analysis pipeline is enabled
:attr:`MuPDF4LLM.LayoutAvailable`                Whether a layout provider is currently registered
:meth:`MuPDF4LLM.SetLayoutProvider`              Register or clear the page layout callback
================================================ =======================================================


**Class API**

.. class:: MuPDF4LLM

  .. method:: SetLayout(bool useLayout)
              SetUseLayout(bool useLayout)

     Enable or disable the layout analysis pipeline used by :meth:`Document.ToMarkdown`, :meth:`Document.ToJson`, :meth:`Document.ToText` and :meth:`Document.ToChunks`.

     :arg bool useLayout: ``true`` turns the pipeline on and registers a layout provider when one is available. ``false`` turns it off (classic extract).

     :meth:`SetUseLayout` is the same method.

     .. code-block:: cs

         using MuPDF.NET;

         MuPDF4LLM.SetLayout(false);
         Document doc = Document.Open("input.pdf");
         string markdown = doc.ToMarkdown(showProgress: false, useOcr: false) ?? "";
         Console.WriteLine($"layoutEnabled={MuPDF4LLM.LayoutEnabled}");
         Console.WriteLine(markdown);

     For layout JSON, text, or chunks, switch the pipeline on and check that a provider is registered:

     .. code-block:: cs

         using MuPDF.NET;

         if (!MuPDF4LLM.LayoutAvailable)
         {
             Console.WriteLine("LAYOUT_UNAVAILABLE");
         }
         else
         {
             MuPDF4LLM.SetLayout(true);
             Document doc = Document.Open("input.pdf");
             string json = doc.ToJson(showProgress: false, useOcr: false);
             Console.WriteLine($"layoutEnabled={MuPDF4LLM.LayoutEnabled}");
             Console.WriteLine(json);
         }

  .. attribute:: LayoutEnabled

     Whether the layout analysis pipeline is currently enabled. Same namespace as :class:`Document`: ``using MuPDF.NET;`` is enough. Same value as :attr:`Utils.LayoutEnabled`. To turn the pipeline on or off, call :meth:`SetLayout`.

     :type: bool

     .. code-block:: cs

         using MuPDF.NET;

         bool prior = MuPDF4LLM.LayoutEnabled;
         MuPDF4LLM.SetLayout(false);
         Console.WriteLine($"layoutEnabled={MuPDF4LLM.LayoutEnabled}");
         MuPDF4LLM.SetLayout(prior);

  .. attribute:: LayoutAvailable

     Whether a layout provider is currently registered. Same namespace as :class:`Document`: ``using MuPDF.NET;`` is enough. :attr:`Utils.LayoutAvailable` is the same value. ``Utils.LayoutPythonAvailable`` is whether the layout worker can be imported.

     :type: bool

     .. code-block:: cs

         using MuPDF.NET;

         if (!MuPDF4LLM.LayoutAvailable)
             Console.WriteLine("LAYOUT_UNAVAILABLE");
         else
             Console.WriteLine($"layoutAvailable={MuPDF4LLM.LayoutAvailable}");

  .. method:: SetLayoutProvider(Func<Page, object> provider)

     Register the callback that supplies page layout boxes, or ``null`` to clear it. Same namespace as :class:`Document`: ``using MuPDF.NET;`` is enough. :meth:`SetLayout` (``true``) registers a provider when the layout worker is available. Most applications do not call this directly.

     :arg Func<Page, object> provider: callback that returns layout boxes for a page, or ``null`` to clear the provider.

     .. code-block:: cs

         using MuPDF.NET;

         MuPDF4LLM.SetLayoutProvider(null);
         Console.WriteLine($"layoutAvailable={MuPDF4LLM.LayoutAvailable}");



See :doc:`32-ToMarkdown </examples/markdown/index>` (classic extract), :doc:`33-ToJsonLayout </examples/json/index>`, :doc:`34-ToText </examples/totext/index>`, :doc:`35-ToChunks </examples/chunks/index>` and :doc:`36-Ocr </examples/ocr/index>`.


.. include:: ../footer.rst
