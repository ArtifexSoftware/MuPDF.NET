.. include:: ../header.rst

.. _WordBlock:

================
WordBlock
================


One word with bbox from :meth:`Page.GetTextWords` / :meth:`TextPage.ExtractWords`.


================================================ =======================================================
**Attribute**                                    **Short Description**
================================================ =======================================================
:attr:`WordBlock.X0`                             Left of the word bbox
:attr:`WordBlock.Y0`                             Top of the word bbox
:attr:`WordBlock.X1`                             Right of the word bbox
:attr:`WordBlock.Y1`                             Bottom of the word bbox
:attr:`WordBlock.Text`                           The word
:attr:`WordBlock.BlockNum`                       Block sequence number
:attr:`WordBlock.LineNum`                        Line sequence number
:attr:`WordBlock.WordNum`                        Word sequence number on the line
================================================ =======================================================


**Class API**

.. class:: WordBlock

   .. attribute:: X0

      Left of the word bounding box.

      :type: float

   .. attribute:: Y0

      Top of the word bounding box.

      :type: float

   .. attribute:: X1

      Right of the word bounding box.

      :type: float

   .. attribute:: Y1

      Bottom of the word bounding box.

      :type: float

   .. attribute:: Text

      The word.

      :type: string

   .. attribute:: BlockNum

      Block sequence number.

      :type: int

   .. attribute:: LineNum

      Line sequence number.

      :type: int

   .. attribute:: WordNum

      Word sequence number on the line.

      :type: int

.. include:: ../footer.rst
