.. include:: ../header.rst

.. _AnnotInfoClass:

================
AnnotInfo
================


Metadata returned by :meth:`Annot.Info`.


================================================ =======================================================
**Attribute**                                    **Short Description**
================================================ =======================================================
:attr:`AnnotInfo.Content`                        Text for Text and FreeText annotations
:attr:`AnnotInfo.Name`                           Icon name
:attr:`AnnotInfo.Title`                          Pop-up window title
:attr:`AnnotInfo.CreationDate`                   Creation timestamp
:attr:`AnnotInfo.ModDate`                        Last modified timestamp
:attr:`AnnotInfo.Subject`                        Subject
:attr:`AnnotInfo.Id`                             Unique annotation id
================================================ =======================================================


**Class API**

.. class:: AnnotInfo

   .. attribute:: Content

      Text for Text and FreeText annotations.

      :type: string

   .. attribute:: Name

      Name of the annotation's icon.

      :type: string

   .. attribute:: Title

      Title of the annotation pop-up window.

      :type: string

   .. attribute:: CreationDate

      Creation timestamp.

      :type: string

   .. attribute:: ModDate

      Last modified timestamp.

      :type: string

   .. attribute:: Subject

      Subject string.

      :type: string

   .. attribute:: Id

      Unique identification of the annotation.

      :type: string

.. include:: ../footer.rst
