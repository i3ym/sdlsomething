#!/bin/bash

/opt/shader-slang-bin/bin/slangc triangle.vert.slang -o triangle.vert.spv
/opt/shader-slang-bin/bin/slangc triangle.frag.slang -o triangle.frag.spv

/opt/shader-slang-bin/bin/slangc triangle-noinst.vert.slang -o triangle-noinst.vert.spv
/opt/shader-slang-bin/bin/slangc triangle-unshaded.frag.slang -o triangle-unshaded.frag.spv
