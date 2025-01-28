
c++
    -o build/shared-release/libmupdfcpp.so.26.0
    -Wl,-soname,libmupdfcpp.so.26.0
    -O2 -DNDEBUG
    -fPIC -shared

    -I /home/maksym/Net/mupdf/include
    -I /home/maksym/Net/mupdf/platform/c++/include
     platform/c++/implementation/classes.cpp platform/c++/implementation/classes2.cpp platform/c++/implementation/exceptions.cpp platform/c++/implementation/extra.cpp platform/c++/implementation/functions.cpp platform/c++/implementation/internal.cpp
    -L build/shared-release -l mupdf -Wl,-rpath,'$ORIGIN',-z,origin
