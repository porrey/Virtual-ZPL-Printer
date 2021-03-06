# Virtual ZPL Printer
An Ethernet based virtual Zebra Label Printer that can be used to test applications that produce bar code labels. This application uses the Labelary service found at [http://labelary.com](http://labelary.com/service.html).

[Download the installer](https://github.com/porrey/Virtual-ZPL-Printer/raw/main/Installer/ZPL%20Virtual%20Printer%20Setup.msi) (v 2.0.1)

Now requires **.NET 6.0**. Download Setup.exe and the MSI if you need to have the .NET 6.0 Framework installed automatically.

###### Version 2.0.1 Updates:
1. Re-factored code/cleanup.
2. Added USPS sample label.
3. Corrected an issue where too many labels at one time could cause a multi-thread related exception.

###### Screen Shots

![](https://github.com/porrey/Virtual-ZPL-Printer/raw/main/Images/VirtualZplPrinter-01.png)

![](https://github.com/porrey/Virtual-ZPL-Printer/raw/main/Images/VirtualZplPrinter-02.png)

![](https://github.com/porrey/Virtual-ZPL-Printer/raw/main/Images/VirtualZplPrinter-03.png)

![](https://github.com/porrey/Virtual-ZPL-Printer/raw/main/Images/VirtualZplPrinter-04.png)

*Screen shots taken in Windows 11*

##### History
###### Version 2.0.0 Updates:
1. Re-factored user interface.
2. Added printer configurations.
3. Added test label templates. Custom templates can be added.
###### Version 1.0.18 Updates:
1. Use Labelary `X-Total-Count` HTTP header to determine the number of labels to create.
###### Version 1.0.17 Updates:
1. Added rotation support.
2. Added support for multiple labels within a single ZPL document.
