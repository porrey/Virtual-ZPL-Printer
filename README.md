# Virtual ZPL Printer
An ethernet based virtual Zebra Label Printer that can be used to test applications that produce bar code labels. This application uses the Labelary service found at [http://labelary.com](http://labelary.com/service.html).

[Download the installer](https://github.com/porrey/Virtual-ZPL-Printer/raw/main/Installer/ZPL%20Virtual%20Printer%20Setup.msi) (v 1.0.18)

Now requires **.NET 6.0**. Download Setup.exe and the MSI if you need to have the .NET 6.0 Framework installed automatically.

###### Version 1.0.18 Updates:
1. Use Labelary `X-Total-Count` HTTP header to determine the number of labels to create.

###### Screenshot

![](https://github.com/porrey/Virtual-ZPL-Printer/raw/main/Images/VirtualZplPrinter.png)

*Screen shot taken in Windows 11*

##### History
###### Version 1.0.17 Updates:
1. Added rotation support.
2. Added support for multiple labels within a single ZPL document.
