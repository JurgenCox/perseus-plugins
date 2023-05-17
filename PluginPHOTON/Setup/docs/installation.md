# Install Python

You have to check one non-default option when installing Python!

1. Download the Python installer from [python.org](https://www.python.org/downloads/).
2. Check the `Add Python to PATH` option on the very first screen.
3. Open a console with administrator rights (see FAQ).
4. Install `photon_ptm` which will automatically install its dependencies.
	
		pip install photon_ptm

4. Make sure the installation was successful by opening Perseus and selecting
   the generic Python processing step. The entry for the Python executable
   should be filled-in automatically and the button should be green.

# Updating packages

Open a console with administrator rights and run

	pip install --upgrade <package name>
# FAQ

1. How do I open a console with administrator rights on Windows?

	Open the start menu and search for `cmd.exe`. Right click and select `Run as administrator`.

2. Who can I check if Python is correctly installed.

	- Open a console
	- `python --version` should give the expected Python version

3. What if Python is already installed but not in the `Path`?

	- Open the environment variable editor as described [here](https://www.computerhope.com/issues/ch000549.htm)
	- Append the Python installation location the the `Path`.
		It should read something like: `C:\WINDOWS\system32;C:\WINDOWS;C:\Program Files\Python 3.6`
