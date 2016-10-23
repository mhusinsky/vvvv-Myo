# vvvv-Myo

Introduction
------------
Node for using the Myo device in VVVV, built upon the MyoSharp wrapper by Nick Cosentino and Tayfun Uzun (https://github.com/tayfuzun/MyoSharp)

Usage
-----
The Myo node exposes most of the functionality MyoSharp offers. This includes
* Receiving gesture data from Myo (rest, fist, wave-in, wave-out, spreaded fingers, double-tap)
* Receiving raw EMG data from Myos sensors
* Receiving gyro and accelerometer-data from Myo
* Sending vibration signals to Myo (short, medium, long)
* Unlocking Myo for enabling gestures 
** timed: unlock for 2 seconds (value is fixed by Myo SDK and cannot be changed)
** hold: unlock until it Myo is locked again manually
* Request and recieve signal strength (RSSI)

Also see the node's help file.

Limitations
-----------
For now this plugin can only use one Myo at a time (and there is no way of setting which Myo it is). I don't have a second Myo to improve and test on this issue so pull requests are welcome ;)

Author
------
Matthias Husinsky, 2016

License
-------
This software is distributed with the [CC Attribution-ShareAlike 4.0](https://creativecommons.org/licenses/by-sa/4.0/) license.
![CC 4.0BY-SA](https://licensebuttons.net/l/by-sa/3.0/88x31.png)