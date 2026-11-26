.. include:: ../header.rst

.. _DeviceWrapper:

================
DeviceWrapper
================


MuPDF device wrapper for pixmap drawing, display-list recording, or text extraction. Used with :meth:`DisplayList.Run` and :meth:`DocumentWriter.BeginPage`.


**Class API**

.. class:: DeviceWrapper

   .. method:: DeviceWrapper(params object[] args)

      Create a device from a :class:`Pixmap` (optional :class:`IRect` clip), a display list, or a text page.

   .. method:: Dispose()

      Release the native device.

   .. method:: ToFzDevice()

      Return the native ``fz_device`` handle.

.. include:: ../footer.rst
