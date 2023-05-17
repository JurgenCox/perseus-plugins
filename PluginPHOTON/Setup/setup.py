from setuptools import setup, find_packages

import os
HERE = os.path.dirname(__file__)
def read(fname):
    return open(os.path.join(HERE, fname)).read()

# creates version_string
exec(open(os.path.join(HERE, "phos", "version.py")).read())

setup(name='photon_ptm',
        version=version_string,
        description='PHOsphoproteomic dissecTiOn via Networks',
        long_description=read('README.md'),
        url='http://www.github.com/jdrudolph/photon',
        author='Jan Rudolph',
        author_email='jan.daniel.rudolph@gmail.com',
        license='MIT',
        packages=find_packages(),
        install_requires=[
            'numpy',
            'pandas>=0.23',
            'scipy',
            'matplotlib',
            'networkx',
            'perseuspy>=0.3.8',
            'flask',
            'requests',
            'celery',
            'redis',
            'goenrich',
            'joblib',
            'scikit-learn',
            'pytest-runner'],
        tests_require=['pytest'])
