# AES Autocrypt
An easy to use console application tool for folder encryption/decryption written in C#. You simply open/execute it on the folder you want to encrypt, follow the instructions in the console, introduce a password, and all the files in the folder start to get encrypted. To decrypt the files you basically do the same, the application will detect there are encrypted files and will give you instructions.

### Features:
- Encrypt and decrypt your files using the secure AES Standard
- Uses multi-threading to avoid CPU bottlenecks (however the bottleneck usually is on the IO)
- Wipes files when encrypted with several passes (This only works for mechanical hdds, if you are using a SSD you will want to disable this) and changes its values to try to remove cached data.
- Portable. Just put the exe in the folder and move the folder around.

### Limitations:
- Can only decrypt/encrypt a folder as a whole. This tool does not support file by file actions.
- The algorithm used encrypts the whole file. This makes big files encyption really slow.

### Encryption standard
This tool uses the Advanced Encryption Standard (AES) with the following defaults:
- Padding mode: PKCS7
- Cipher mode: CBC
- Salt: 8 bytes HEX[c4, 0a, 7f, 4f, a2, c0, 92, 37]
- Key size: 256-bit \*\*
- Block size: 128-bit \*\*
- 7000 iterations

\*\* may vary depending on the .NET version

### Should I use this tool?
I have used this application in several occasions without any issues. However a more mature and thoroughly tested tool is probably a better idea. Take this more as an educational example.

### License (public domain)
This is free and unencumbered software released into the public domain.

Anyone is free to copy, modify, publish, use, compile, sell, or
distribute this software, either in source code form or as a compiled
binary, for any purpose, commercial or non-commercial, and by any
means.

In jurisdictions that recognize copyright laws, the author or authors
of this software dedicate any and all copyright interest in the
software to the public domain. We make this dedication for the benefit
of the public at large and to the detriment of our heirs and
successors. We intend this dedication to be an overt act of
relinquishment in perpetuity of all present and future rights to this
software under copyright law.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
IN NO EVENT SHALL THE AUTHORS BE LIABLE FOR ANY CLAIM, DAMAGES OR
OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE,
ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
OTHER DEALINGS IN THE SOFTWARE.

For more information, please refer to <http://unlicense.org/>