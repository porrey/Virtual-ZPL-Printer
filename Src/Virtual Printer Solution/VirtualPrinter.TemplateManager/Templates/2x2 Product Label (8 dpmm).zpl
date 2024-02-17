^XA

^FX Draw a border around the edge
^FO 5,5
^GB 396.4,396.4,2,B,0
^FS

^FX Display th company name
^FO 15,30
^A0,27,30
^FD ACME Printing Solutions
^FS

^FX Display the product name
^FO 15,70
^A0,27,25
^FD WIDGETS
^FS

^FX Display a QR code with a link back to the company
^FO 230,65
^BQ N,2,4,M,7
^FD https://github.com/porrey/Virtual-ZPL-Printer
^FS

^FX Display a border around the item number
^FO 25,125
^GB 160,100.4,2,B,0
^FS

^FX Display the item number
^FO 40,145
^A0,80,80
^FD {id2}
^FS

^FX Display the item serial number bar code
^FO 65,245
^BY 2,2.0
^BC N,120,Y,N,N,N
^FD {id8}
^FS

^XZ