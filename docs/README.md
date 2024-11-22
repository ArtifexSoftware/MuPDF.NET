# MuPDF.NET documentation

Welcome to the documentation. This documentation relies on [Sphinx](https://www.sphinx-doc.org/en/master/) to publish HTML docs from markdown files written with [restructured text](https://en.wikipedia.org/wiki/ReStructuredText) (RST).


## Sphinx version

This README assumes you have [Sphinx v5.0.2 or above installed](https://www.sphinx-doc.org/en/master/usage/installation.html) on your system.


## Updating the documentation

Within `docs` update the associated restructured text (`.rst`) files. These files represent the corresponding document pages. 


## Building HTML documentation

- Ensure you have the `furo` theme installed:

`pip install furo`

- Ensure you have `Sphinx design` installed:

`pip install sphinx-design`

- Ensure you have `Sphinx Copy Button` installed:

`pip install sphinx-copybutton`

- Ensure you have `Sphinx Tabs` installed:

`pip install sphinx-tabs`

- Ensure you have `Sphinx Not Found` component installed:

`pip install sphinx-notfound-page`

- Ensure you have `Google Analytics` component installed:

`pip install sphinxcontrib-googleanalytics`

- From the "docs" location run:

`sphinx-build -b html . build/html`

This then creates the HTML documentation within `build/html`. 

> Use: `sphinx-build -a -b html . build/html` to build all, including the assets in `_static` (important if you have updated CSS).


- Alternatively you can also use [Sphinx Autobuild](https://pypi.org/project/sphinx-autobuild/) and do:

`sphinx-autobuild . _build/html`

This will ensure that the documentation runs in a localhost and will also hot-reload changes.

## Windows

On Windows you may need to trigger Sphinx builds with more dedicated, direct commands, for example:

Check Sphinx is installed & version:

python -m sphinx sphinx-build --version

Build the docs:

python -m sphinx -a -b html . build/html


---


For full details see: [Using Sphinx](https://www.sphinx-doc.org/en/master/usage/index.html) 



