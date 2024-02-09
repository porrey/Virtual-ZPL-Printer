^XA

^FX ImageFileName: USPS-Shipping
^FX Set label size to 4" x 6" (assumes 8 dpmm)
^LL 1218
^PW 812

^FX Set the Font/Encoding to
^FX Unicode (UTF-8 encoding) - Unicode Character Set
^CI 28

^FX Shift the label up and to the right
^LT 0
^LS 0

^FX Vertical line for service icon block
^FO 203.2,0
^GB 3,203,3,B
^FS

^FX Service Block (P Priority Mail)
^FO 0,20
^FB 208,1,0,C,0
^A0N,210,210
^FDP\&
^FS

^FX Payment Block - 20mm 
^FO 533,25.6
^GB 228,152,2,B
^FS

^FX Payment Block Line 1
^FO 533,56
^FB 228,1,0,C,0
^A0N,20,20
^FDPRIORITY MAIL\&
^FS

^FX Payment Block Line 2
^FO 533,80
^FB 228,1,0,C,0
^A0N,20,20
^FDU.S. POSTAGE PAID\&
^FS

^FX Payment Block Line 3
^FO 533,104
^FB 228,1,0,C,0
^A0N,20,20
^FDACME WIDGETS\&
^FS

^FX Payment Block Line 4
^FO 533,128
^FB 228,1,0,C,0
^A0N,20,20
^FDeVS\&
^FS

^FX Top horizontal line
^FO 0,203.2
^GB 812,4,4,B
^FS

^FX Service Text
^FO 0,219.2
^FB 750,1,0,C,0
^A0N,52,61
^FDUSPS PRIORITY MAIL\&
^FS

^FX Registered Trademark symbol
^FO 650,215.2
^GS N,65,65
^FDA
^FS

^FX Bottom horizontal line
^FO 0,267.2
^GB 812,4,4,B
^FS

^FX Return Address Line 1
^FO 15,286
^A0N,24,24
^FDINTERNET SALES DEPARTMENT
^FS

^FX Return Address Line 2
^FO 15,314
^A0N,24,24
^FDFAST AND EFFICIENT SUPPLY CO.
^FS

^FX Return Address Line 3
^FO 15,342
^A0N,24,24
^FD10474 COMMERCE BVLD DUPLEX B
^FS

^FX Return Address Line 4
^FO 15,370
^A0N,24,24
^FDSILVER SPRING MD20910-9999
^FS

^FX Delivery Address Line 1
^FO 112,533
^A0N,29,29
^FDRONALD RECEIVER
^FS

^FX Delivery Address Line 2
^FO 112,566
^A0N,29,29
^FDC/O RICK RECIPIENT
^FS

^FX Delivery Address Line 3
^FO 112,599
^A0N,29,29
^FDINTERNET PURCHASING OFFICE
^FS

^FX Delivery Address Line 4
^FO 112,632
^A0N,29,29
^FDBIG AND GROWING BUSINESS, CO.
^FS

^FX Delivery Address Line 5
^FO 112,665
^A0N,29,29
^FD8403 LEE HIGHWAY
^FS

^FX Delivery Address Line 6
^FO 112,698
^A0N,29,29
^FDMERRIFIELD VA 22082-9999
^FS

^FX IM Barcode Top Line
^FO 0,811.2
^GB 812,15,15,B
^FS

^FX USPS Tracking Text
^FO 0,837.2
^FB 812,1,0,C,0
^A0N,32,32
^FDUSPS SIGNATURE TRACKING #\&
^FS

^FX IM Barcode
^FO 25,888.2
^BY 3
^BC N,152,N,N,N,D
^FD4202208<89205591234{id2}0{id3}{id4}01
^FS

^FX IM Readable Text
^FO 0,1066
^FB 812,1,0,C,0
^A0N,32,32
^FD9205 5912 34{id2} 0{id3} {id4} 01\&
^FS

^FX IM Barcode Bottom Line
^FO 0,1102.2
^GB 812,15,15,B
^FS

^FX Reset
^LT 0
^LS 0

^XZ