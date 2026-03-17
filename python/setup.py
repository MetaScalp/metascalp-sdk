from setuptools import setup, find_packages

setup(
    name='metascalp',
    version='1.0.0',
    description='Official SDK for MetaScalp API — connect trading bots and scripts to MetaScalp terminal',
    packages=find_packages(),
    python_requires='>=3.8',
    install_requires=['aiohttp>=3.8', 'websockets>=11.0'],
    license='MIT',
    url='https://github.com/grigoryeghiazaryan/metascalp-sdk',
)
