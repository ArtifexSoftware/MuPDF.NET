.. include:: ../header.rst

.. _TextBlock:

================
TextBlock
================


One text or image block from :meth:`Page.GetTextBlocks` / :meth:`TextPage.ExtractBlocks`.


================================================ =======================================================
**Attribute**                                    **Short Description**
================================================ =======================================================
:attr:`TextBlock.X0`                             Left of the block bbox
:attr:`TextBlock.Y0`                             Top of the block bbox
:attr:`TextBlock.X1`                             Right of the block bbox
:attr:`TextBlock.Y1`                             Bottom of the block bbox
:attr:`TextBlock.Text`                           Block text
:attr:`TextBlock.BlockNum`                       Block sequence number
:attr:`TextBlock.Type`                           0 = text, 1 = image
================================================ =======================================================


**Class API**

.. class:: TextBlock

   .. attribute:: X0

      Left of the block bounding box.

      :type: float

   .. attribute:: Y0

      Top of the block bounding box.

      :type: float

   .. attribute:: X1

      Right of the block bounding box.

      :type: float

   .. attribute:: Y1

      Bottom of the block bounding box.

      :type: float

   .. attribute:: Text

      Block text (for an image block, image meta information — not the image bytes).

      :type: string

   .. attribute:: BlockNum

      Block sequence number.

      :type: int

   .. attribute:: Type

      1 for an image block, 0 for text.

      :type: int

.. include:: ../footer.rst
