.. include:: ../header.rst

.. _Barcode:

================
Barcode
================


Class representing a barcode object.


================================== =======================================================
**Method / Attribute**             **Short Description**
================================== =======================================================
:meth:`Barcode.putMetadata`        Add metadata to the barcode
:meth:`Barcode.putAllMetadata`     Add a listing of metadata to the barcode
:meth:`Barcode.addResultPoints`    Adds result points to the barcode
:meth:`Barcode.ToString`           Gets the text value or number of bytes as a string from the barcode
:attr:`Barcode.Text`               The text value for the barcode
:attr:`Barcode.RawBytes`           The barcode's raw bytes
:attr:`Barcode.ResultPoints`       The barcode's points
:attr:`Barcode.BarcodeFormat`      The barcode's format
:attr:`Barcode.BarcodeMetadata`    The barcode's metadata
:attr:`Barcode.Timestamp`          The barcode's timestamp
:attr:`Barcode.NumBits`            The barcode's number of bits
================================== =======================================================



.. note::

   See :ref:`Working with Barcodes <The_Basics_Working_With_Barcodes>` for usage examples.

**Class API**

.. class:: Barcode



   .. method:: Barcode(string text, byte[] rawBytes, int numBits, BarcodePoint[] resultPoints, BarcodeFormat format, long timestamp)

      Creates a *Barcode* object.

      :arg string text: String value for barcode.
      :arg byte[] rawBytes: Raw bytes value for barcode.
      :arg int numBits: Number of bits for barcode.
      :arg BarcodePoint[] resultPoints: Result pointsfor barcode.
      :arg BarcodeFormat format: :ref:`BarcodeFormat <Barcode_BarcodeFormat>` for barcode.
      :arg long timestamp: Timestamp for barcode.


   .. method:: putMetadata(BarcodeMetadataType type, object value)

      Adds metadata to the barcode

      :arg BarcodeMetadataType type: Type of barcode.
      :arg object value: A key/value object for the meatadata.


   .. method:: putAllMetadata(IDictionary<BarcodeMetadataType, object> metadata)

      Adds metadata from a dictionary listing to the barcode.

      :arg IDictionary metadata: Dictionary of metadata.


   .. method:: addResultPoints(BarcodePoint[] newPoints)

      Adds result points to a barcode.

      :arg BarcodePoint[] newPoints: An array of barcode points.


   .. method:: ToString()

      Returns the atring value of the barcode text, or if `null` then the bytes length.

      :rtype: `string`
      :returns: the string value of the text or the bytes length as a string.


   .. attribute:: Text

      The text value for the barcode. Get only.

      :type: `bool`

   .. attribute:: RawBytes

      The barcode's raw bytes. Get only.

      :type: `byte[]`

   .. attribute:: ResultPoints

      The barcode's points. Get only.

      :type: `BarcodePoint[]`

   .. attribute:: BarcodeFormat

      The barcode's format. Get only.

      :type: :ref:`BarcodeFormat <Barcode_BarcodeFormat>`

   .. attribute:: BarcodeMetadata

      The barcode's metadata. Get only.

      :type: `IDictionary<BarcodeMetadataType, object>`

   .. attribute:: Timestamp

      The barcode's timestamp. Get only.

      :type: `long`


   .. attribute:: NumBits

      The barcode's number of bits. Get only.

      :type: `int`




.. _Barcode_BarcodeFormat:



BarcodeFormat
---------------------------

Barcode formats are available from the ``BarcodeFormat`` enumeration:


.. list-table::
   :header-rows: 1

   * - Barcode type
   * - ``ALL`` = 0
   * - ``AZTEC``
   * - ``BOXES``
   * - ``CODABAR``
   * - ``CODABLOCKF``
   * - ``CODE128``
   * - ``CODE16K``
   * - ``CODE39``
   * - ``CODE39_LINEARREADER``
   * - ``CODE39_EX``
   * - ``CODE39_NOISE1``
   * - ``CODE93``
   * - ``DM``
   * - ``DM_DPM``
   * - ``EAN13``
   * - ``EAN2``
   * - ``EAN5``
   * - ``EAN8``
   * - ``EAN_UPC_OLD``
   * - ``GS1DATABAREXP``
   * - ``GS1DATABAREXPSTACKED``
   * - ``GS1DATABAROMNI``
   * - ``GS1DATABARSTACKED``
   * - ``GS1DATABARSTACKEDOMNI``
   * - ``GS1DATABARLIMITED``
   * - ``HORIZONTALLINES``
   * - ``I2OF5``
   * - ``IM``
   * - ``KIX``
   * - ``LINETABLES``
   * - ``MAXICODE``
   * - ``MICR``
   * - ``MICROPDF``
   * - ``MSI``
   * - ``OMRCIRCLE``
   * - ``OMRCIRCLE_EXT``
   * - ``OMROVAL``
   * - ``OMROVAL_EXT``
   * - ``OMRSQUARE``
   * - ``OMRSQUARE_EXT``
   * - ``OMRSQUARELPATTERN``
   * - ``OMRRECTANGLE``
   * - ``OMRRECTANGLE_EXT``
   * - ``OMRRECTANGLELPATTERNVERT``
   * - ``OMRRECTANGLELPATTERNHORIZ``
   * - ``PATCH``
   * - ``PHARMA``
   * - ``PDF417``
   * - ``POSTCODE``
   * - ``POSTNET``
   * - ``QR``
   * - ``RAWOMR``
   * - ``RM``
   * - ``VERTICALLINES``
   * - ``UPC_A``
   * - ``UPC_E``
   * - ``TRIOPTIC``




.. include:: ../footer.rst
