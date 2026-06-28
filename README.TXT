================================================================
  ARCHIVED PROJECT
================================================================
This repository is archived and kept for historical reference.
It is no longer actively developed.

Two successor projects carry these ideas forward:

  TalkTeach ASR - offline, cross-platform desktop app with an
  easy-to-use training wizard for state-of-the-art ASR models:
    https://github.com/sicambria/talkteach-asr

  SpeechAngel - on-device, trainable, language-independent voice
  control for Android (speaker-dependent MFCC + DTW matching, the
  direct descendant of Turan), built for accessibility:
    https://github.com/sicambria/speechangel

================================================================

Turán RMS (Recognition Made Simple)
GNU General Public Licence (GNU GPL) v3

https://github.com/sicambria
sicambria@users.sourceforge.net
-------------------------------------------------------
Recent changes (high level)
-------------------------------------------------------
-Fixed correctness bugs in the recognition core: unified the DTW
 feature-vector width, added a rejection sentinel, and repaired
 the HTK feature-file reader.
-Repaired DSP math: restored pre-emphasis, corrected signal
 processing, and removed dead native code paths.
-Completed and quarantined a CoMIRVA-based native MFCC port as
 unused reference material.
-Verified the bundled licenses and documented the project's
 limitations.
-------------------------------------------------------
v1.0
2011-01-03

-Build modules and update references
-On x64 systems, set architecture to x86
-LPC mode recommended
-You'll need the HTK toolkit for the best MFCC results (HCopy.exe):
 http://htk.eng.cam.ac.uk/
-------------------------------------------------------
v1.1
2015-06-05

-Compiled in Visual Studio 2013
-------------------------------------------------------
