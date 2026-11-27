.. include:: ../header.rst

================
LinkDest
================


Resolved link or outline destination. Built from a :class:`Link`, :class:`Outline`, or URI fragment.


================================================ =======================================================
**Attribute**                                    **Short Description**
================================================ =======================================================
:attr:`LinkDest.Kind`                            Link kind (``LINK_*``)
:attr:`LinkDest.Page`                            Target page number
:attr:`LinkDest.Uri`                             URI fragment or external URI
:attr:`LinkDest.Lt`                              Top-left point
:attr:`LinkDest.Rb`                              Bottom-right point
:attr:`LinkDest.FileSpec`                        External file spec
:attr:`LinkDest.Flags`                           Destination flags
:attr:`LinkDest.IsMap`                           Whether the destination is a map
:attr:`LinkDest.IsUri`                           Whether the destination is a URI
:attr:`LinkDest.Named`                           Named-destination parameters
:attr:`LinkDest.NewWindow`                       New-window flag
:attr:`LinkDest.DestStr`                         Reserved destination string
================================================ =======================================================


**Class API**

.. class:: LinkDest

   .. attribute:: Kind

      Link kind (``LINK_*``).

      :type: int

   .. attribute:: Page

      Target page number.

      :type: int

   .. attribute:: Uri

      URI fragment or external URI.

      :type: string

   .. attribute:: Lt

      Top-left point for zoom destinations.

      :type: :class:`Point`

   .. attribute:: Rb

      Bottom-right point.

      :type: :class:`Point`

   .. attribute:: FileSpec

      External file spec.

      :type: string

   .. attribute:: Flags

      Destination flags.

      :type: int

   .. attribute:: IsMap

      Whether the destination is a map.

      :type: bool

   .. attribute:: IsUri

      Whether the destination is a URI.

      :type: bool

   .. attribute:: Named

      Named destination parameters.

      :type: Dictionary<string, object>

   .. attribute:: NewWindow

      New-window flag.

      :type: string

   .. attribute:: DestStr

      Reserved destination string.

      :type: string

.. include:: ../footer.rst
