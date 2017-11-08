v4.0.5 with single site and multisite supporting, double confirm trim code

different with v4.0.4
1. add delay_sync between singlepath setting,
2. change repower delay time from delay_operation to delay_sync,
3. set M.R.E when spin.

Features:
High precision,
Faster version( ~16s per unit).


v4.0.6 with single site and multisite supporting, double confirm trim code

different with v4.0.5
1. add reconnect buttom in EngTab,
2. For autotrim_singlesite(), add a judgement that if any master bit be trimmed trim agian.
3. binXaccuracy set to double.
4. update PreTrim tab binXaccuracy when start up.

v4.0.7 with IT6512 current supply automatic control

different with v4.0.6
1. IT6512 current supply automatic control,
2. add adcoffset in AutoTab,
3. display chosen gain and adcoffset in AutoTab,
4. add IPON and IPOFF in EngTab.

v4.0.8 remove MRE display

different with v4.0.7
1. remove MRE display,
2. restore measure current before autotrim.
3. remove '\r\n' in log

v4.0.9 support multi-site automatic current control

different with v4.0.7
1. multi-site automatic current control
2. change to release version, cause debug version's running speed will be low down after hours.

v4.1.0 add delay during multisite select to overcome some case reload fail.
1. add delay during multisite select,
2. change delay_power to 80ms,
3. retrim if master is 0 when relaod.

*******************************************************************************************************************

v4.2.0 re-struct delay and signal path control
1. add log clear
2. add automatin and manual selection

v4.2.2 change current of power supply output from 2V to 6V for more power ability
1. change current of power supply output from 2V to 6V
2. add 'MOA' and 'MPE' display for signle-site automation trim,
moa---module output abnormal,
mpe---module position error.

v4.2.3 fix no need trim part bug
1. if no need trim, will fuse and return directly.

v4.2.4 fix bug of v4.2.3 
1. after no need trim case, vdd is stay at 6V, so next round can't measure IQ successfully.
so at the begain of each round, confirm vdd is set at 5V.

V4.2.5 Internal version
1. add Safety read,
2. remove all masks of Marginal and Safety read.

v4.2.6 Internal version
1.	add ModuleAttribute struct,
2.	print ModuleAttribute when return,
3.	add safety read and marginal read mask indicator, 
ERROR CODE:
1.	IQ abnormal -> Try Again;
2.	Saturation -> MOA;
3.	Lower Sensitivity -> MOA;
4.	Trimmed -> FAIL;
5.	DigitalCommFail -> FAIL;
6.	VIP ~=V0A -> Try Again;
7.	VIP<VOA->Try Again;
8.	V0A<2.25 || VOA > 2.8 -> FAIL

v4.2.7 Internal version
1. Fix saturation, add 50% gain will case module saturation if Vout@0A have reach to ~100mv positive offset;
2. Fix load config dispaly bug, change "******** time ********" to <------- time ------->.

v4.2.9 add communication test button and brake tab

v4.3.0 fix saturation bugs
1. fix PreTrim Vout measurement not accurate bug, by add delay after enter normal mode,
2. increase delay_fase time from 300ms to 400ms,
3. decrease PreSet gain of Vout at second time,
4. change saturation threshhold from 4.85 to 4.9V,
5. change saturation threshhold as well when change offset value of vout.

v4.3.1 fix gain accurate at 1.65V offset


v4.3.2 support 1.65V offset
1. add trim code table for 1.65V,
2. select trim code table when choose offset,
3. modify target_offset to Target_offset
