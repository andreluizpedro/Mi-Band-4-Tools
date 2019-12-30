# Mi Band 4 Tools

## Font Tool v1.0

For unpacking and re-packing .ft files.
<br>
Supports:
* NEZK 1
* NEZK 2

### Unpacking

Unpacks the font into ```bmp-24\``` and ```bmp-16\``` in the parent directory of the font file.
<br>
```
> FontTool.exe unpack <path_to_file>
```

### Packing

Packs the font from ```bmp-24\``` and ```bmp-16\``` to the new font file.
```
> FontTool.exe pack <version> <path_to_new_file>
```
**The directories need to be in the parent directory of the new font!**
