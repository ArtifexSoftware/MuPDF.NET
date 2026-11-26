.. include:: ../header.rst

.. _LayerConfigUI:

================
LayerConfigUI
================


One user-interface item from :meth:`Document.LayerUIConfigs` for optional content that a PDF viewer can show or hide.


================================================ =======================================================
**Attribute**                                    **Short Description**
================================================ =======================================================
:attr:`LayerConfigUI.Number`                     Running sequence number
:attr:`LayerConfigUI.Text`                       Label or originating OCG name
:attr:`LayerConfigUI.Type`                       ``label``, ``checkbox``, or ``radiobox``
:attr:`LayerConfigUI.Depth`                      Nesting level in ``/Order``
:attr:`LayerConfigUI.On`                         Current on/off state
:attr:`LayerConfigUI.IsLocked`                   True if the UI cannot change it
================================================ =======================================================


**Class API**

.. class:: LayerConfigUI

   .. attribute:: Number

      Running sequence number.

      :type: int

   .. attribute:: Text

      Text string or name field of the originating :data:`OCG`.

      :type: string

   .. attribute:: Type

      One of ``"label"`` (set by a text string), ``"checkbox"`` (a single OCG), or ``"radiobox"`` (connected OCGs).

      :type: string

   .. attribute:: Depth

      Nesting level in the ``/Order`` array.

      :type: int

   .. attribute:: On

      Item state.

      :type: bool

   .. attribute:: IsLocked

      True if the item cannot be changed via user interfaces.

      :type: bool

.. include:: ../footer.rst
