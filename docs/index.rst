.. meta::
   :description: MuPDF.NET Developer documentation.
   :keywords: mupdf, .net, pdf, document, api, split, merge, extract, view

:html_theme.sidebar_secondary.remove:

.. adds an engaging background image to the landing page
.. raw:: html

    <script>
        let bg = document.getElementsByClassName("bd-container")[0];
        bg.classList.add("landing-page");
        let div = document.createElement("div");
        bg.prepend(div);

        var spans = "";
        for (var i=0;i<11;i++) {
            spans += "<span></span>";
        }
        
        div.innerHTML = "<div class='bokeh-background'>"+spans+"</div>";

    </script>


.. |MuPDF.NET| raw:: html

    <cite>MuPDF.NET</cite>


.. |PyMuPDF| raw:: html

    <cite>PyMuPDF</cite>


Welcome to |MuPDF.NET|
==================================================

**MuPDF** for **.NET**.

|MuPDF.NET| is a high-performance **C#** library for data extraction, analysis, conversion & manipulation of **PDF** (and other) documents.

|MuPDF.NET| is hosted on `GitHub <https://github.com/ArtifexSoftware/MuPDF.NET>`_ and is registered on `NuGet <https://www.nuget.org/packages/MuPDF.NET/>`_.

|MuPDF.NET| has been derived from `PyMuPDF <https://github.com/pymupdf/PyMuPDF>`_ and it closely follows its structure. So for most of |PyMuPDF|'s classes, features and functions you will find a respective |MuPDF.NET| object doing the same sort of thing.

Pease note that there are deviations as per how objects are named. We will publish a table in due time that maps |MuPDF.NET| and |PyMuPDF| names.

The following is a list of the most important features:

- Fast rendering of **PDF** files
- Extract text and search **PDF** files
- **PDF** editing & annotations
- Get **PDF** metadata information
- Manage **PDF** passwords
- And more!


Developer documentation to help you get started 
--------------------------------------------------------------------------------------------------


.. adds a class to a section
.. rst-class:: hide-me

.. toctree::
    :caption: Welcome to MuPDF.NET Documentation
    :maxdepth: 2

    getting-started/index.rst

.. toctree::
   :caption: API Reference
   :maxdepth: 2

   api/index.rst


.. include:: footer.rst


.. The home page doesn't need to show the feedback form in the footer
.. raw:: html

    <script>document.getElementById("feedbackHolder").style.display = "none";</script>
