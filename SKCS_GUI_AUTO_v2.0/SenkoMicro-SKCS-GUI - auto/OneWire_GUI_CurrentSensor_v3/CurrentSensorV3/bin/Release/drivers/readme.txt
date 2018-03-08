"USB Host Windows Drivers"

Date Created: 10/17/07 

This directory contains USB Windows host drivers and information
files. 

The files are included in the directory structure shown below:

+---Blackfin
    +---Examples
    ¦   +---usb
    ¦       +---host
    ¦           +---windows 	
    ¦               +---drivers
    ¦                      bulkadi.inf
    ¦		           bulkadi.sys
    ¦		           readme.txt

__________________________________________________________

	
CONTENTS

I.    Overview
II.   Building Bulkadi


I. Overview

This folder contains USB Windows host drivers.


II. Building Bulkadi

Bulkadi is based on the Microsoft bulkusb example for which we are not allowed
to ship the source code.  Therefore in order to rebuild bulkadi you will need
the Microsoft Windows 2003 Server DDK.  The DDK's bulkusb example was used to
create the bulkadi example host driver.  The Windows 2003 Server DDK is available
from Microsoft.  For more information on obtaining the DDK visit:
http://www.microsoft.com/whdc/devtools/ddk/default.mspx.

The following changes were made to bulkusb, located in <WINDDK>\3790.1830\src\wdm\usb
in order to create the bulkadi driver.  Follow these steps closely in order to create
your own custom bulk driver.

0. Install the DDK.  Note, during the install, make sure to include the WDM Samples
   while selecting the Driver Development Kit Component Groups to include the bulkusb
   sample driver.
1. Copy all the files in the bulkusb folder to a new folder named bulkadi and make
   all your changes in the bulkadi folder.
2. In file bulkusb.h, BULKUSB_MAX_TRANSFER_SIZE was changed from 256 to 64k (64*1024).
   Leaving it unchanged greatly reduces the performance.(Also, make sure to use the 
   paranthesis).
3. In file bulkusb.h, the end of the define for BULKUSB_REGISTRY_PARAMETERS_PATH was
   changed from "BULKUSB\\Parameters" to "BULKADI\\Parameters" so that the new driver
   doesn't reference bulkusb's information.
4. In file bulkusb.rc, all three references to bulkusb were changed to bulkadi.  This
   will change the driver detail information such as file description and filename.
5. In file bulkusr.h, the DEFINE_GUID macro is used to define the sample GUID named
   GUID_CLASS_I82930_BULK.  we changed the name to GUID_BULKADI.  If creating a custom
   bulk driver: use the "guidgen.exe" utility to create a new GUID.  "guidgen.exe" is
   included in the DDK package at <WINDDK>\3790.1830\tools\other\i386.  Each driver must
   use a unique GUID which can be created with guidgen.exe.  This newly created GUID
   should be used by your host application to open a handle to the driver.  Otherwise
   use the GUID that has already been created by adi under:
   <VDSP>\Blackfin\Examples\usb\host\windows\hostapp\adiguid.h
6. In file bulkusb.c, the GUID used in step #5 is referenced in function
   BulkUsb_AddDevice().  If you changed the name of the GUID in step #5 you must also
   change the reference to the GUID in this file to the same name (such as to
   GUID_BULKADI).
7. In file bulkpnp.c, Interface->Pipes[i].MaximumTransferSize is changed to
   BULKUSB_MAX_TRANSFER_SIZE in function SelectInterfaces().
8. In file bulkwmi.c, another GUID is defined named BULKUSB_WMI_STD_DATA_GUID.  We
   once again used guidgen.exe to create a new GUID but left the name unchanged.  This
   is for Windows Management Instrumentation (WMI) support.
9. In file sources, we changed the TARGETNAME from bulkusb to bulkadi.
10. Note, we use our own INF file so the one supplied with bulkusb (bulkusb.inf) is not
    used.
11. From the Windows Start menu, select "Programs->Development Kits->Windows DDK 3790.1830->
    Build Environments->Windows XP", then choose the desired build environment,either the 
    "Windows XP Free Build Environment" (release) or the "Windows XP Checked Build Environment"
    (debug).It is generally recommended to rebuild in both the environments.
12. In the console window that appears change directory to the bulkadi folder by
    entering "cd src\wdm\usb\bulkadi" at the command prompt.
13. To build enter "build -cZ" at the command prompt.  Note the lowercase 'c' (delete
    all object files) and the uppercase 'Z' (no dependency checking or scanning of
    source files with three-passes) are case-sensitive.
14. The driver (.sys) will be output to either the free or checked folder depending on
    build environment selected. The driver file is about 18kb in free (release) mode, and 
    about 48k in checked (debug) mode

