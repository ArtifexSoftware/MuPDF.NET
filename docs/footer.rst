----

.. raw:: html

   <script>

      // On Load, do what we need to do with the DOM
      document.body.onload = function() {

         // ensure external links open in a new tab
         const collection = document.getElementsByClassName("nav-external");
         for (var i=0;i<collection.length;i++) {
            collection[i].setAttribute("target", "blank");
         }

         const collectionB = document.getElementsByClassName("reference external");
         for (var i=0;i<collectionB.length;i++) {
            collectionB[i].setAttribute("target", "blank");
         }

         // set the copyright
         const footerItem = document.getElementsByClassName("footer-item");
         for (var i=0;i<footerItem.length;i++) {
            const copyright = footerItem[i].getElementsByClassName("copyright");
            for (var j=0;j<copyright.length;j++) {
               copyright[j].innerHTML = "&copy; Copyright 2024 <a href='https://artifex.com' target=_blank>Artifex Software, Inc</a> — All Rights Reserved";
            }
         }

         const footerItemEnd = document.getElementsByClassName("footer-items__end");
         for (var i=0;i<footerItemEnd.length;i++) {
            const endItem = footerItemEnd[i];
            endItem.innerHTML = "<a href='https://discord.gg/DQ8GBG6V4g' target='new'>Support</a>";
         }

      };


      function gotoPage(page) {
         window.location.href = page;
      }


   </script>




.. external links

.. _the GitHub project: https://github.com/ArtifexSoftware/MuPDF.NET
.. _Nuget:
.. _MuPDF.NET package from NuGet: https://www.nuget.org/packages/MuPDF.NET/
.. _MuPDF Blog: https://forum.mupdf.com
.. _contact Artifex: https://artifex.com/contact/mupdf-net-inquiry.php
.. _Artifex Community License: https://github.com/ArtifexSoftware/MuPDF.NET/blob/main/LICENSE.md
