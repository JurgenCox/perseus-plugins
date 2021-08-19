try:
    import warnings, os, sys, time
    warnings.filterwarnings('ignore')
    print('Installing necessary packages...')
    time.sleep(3)
    executable = os.path.dirname(sys.executable)
    piploc = os.path.join(sys.prefix, 'Scripts')
    os.chdir(piploc)
    sep = os.path.sep
    os.system(f'.{sep}pip install --user -q numpy --disable-pip-version-check')
    os.system(f'.{sep}pip install --user -q perseuspy --disable-pip-version-check')
    os.system(f'.{sep}pip install --user -q biopython --disable-pip-version-check')
    print('finish install. Testing import...')
    import numpy, perseuspy, Bio
except Exception as e:
    print(e)
    time.sleep(5)
    exit(1)
