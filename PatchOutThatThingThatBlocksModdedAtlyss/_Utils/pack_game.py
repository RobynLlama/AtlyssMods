import os
import sys
import glob
import subprocess
import re
import shutil
import zipfile

def keep_assembly_dependency(assembly):
  IGNORED_ASSEMBLIES = [
    "AsmResolver",
    "BepInEx.AssemblyPublicizer",
    "Microsoft.Win32",
    "Newtonsoft.Json",
    "System.",
  ]

  for part in IGNORED_ASSEMBLIES:
    if part in assembly:
      return False

  return True


LAST_WORKDIR = os.getcwd()

# Ensure script location is current directory
os.chdir(os.path.dirname(os.path.abspath(__file__)))

# Let's rebuild the project to make sure it's all good
os.chdir('..')

csprojs = glob.glob("*.csproj")

if len(csprojs) != 1:
  print("Couldn't determine project file to build.")
  sys.exit(1)

# Uncomment in case you're running this standalone and need to build the project
# subprocess.run(['dotnet', 'build', csprojs[0]])

if len(csprojs) == 1:
  with open(csprojs[0], 'r') as f:
    CSPROJ_DATA = f.read()
    MATCH = re.search("<TargetFramework>(.*)</TargetFramework>", CSPROJ_DATA)

    if MATCH is not None:
      if MATCH.group(1) == 'netstandard2.1':
        FRAMEWORK = 'netstandard2.1'
      elif MATCH.group(1) == 'net47':
        FRAMEWORK = 'net47'
      elif MATCH.group(1) == 'net471':
        FRAMEWORK = 'net471'
      elif MATCH.group(1) == 'net472':
        FRAMEWORK = 'net472'
      elif MATCH.group(1) == 'net48':
        FRAMEWORK = 'net48'
      elif MATCH.group(1) == 'net481':
        FRAMEWORK = 'net481'
      else:
        # Let's assume it's netstandard2.1
        FRAMEWORK = 'netstandard2.1'
    else:
      # Let's assume it's netstandard2.1
      FRAMEWORK = 'netstandard2.1'

    MATCH_NAME = re.search("<AssemblyName>(.*)</AssemblyName>", CSPROJ_DATA)
    
    if MATCH is not None:
      MOD_NAME = MATCH_NAME.group(1)
    else:
      MOD_NAME = 'Mod'
else:
  # Let's assume it's netstandard2.1
  FRAMEWORK = 'netstandard2.1'

os.chdir(os.path.join('bin', 'Debug', FRAMEWORK))
ASSEMBLIES = [os.path.abspath(path) for path in glob.glob('*.dll') if keep_assembly_dependency(path)]

if len(ASSEMBLIES) == 0:
  print('Couldn\'t find build files, please check that the project has been built in Debug mode.')
  sys.exit(1)

print('Will copy the following DLLs to the package:')

for assembly in ASSEMBLIES:
  print(' - ' + assembly)

os.chdir(os.path.join('..', '..', '..'))
os.chdir('_Thunderstore')

shutil.rmtree('Output', ignore_errors=True)

os.mkdir('Output')
  
# Pack everything into a zip

with zipfile.ZipFile(os.path.join('Output', f'{MOD_NAME}.zip'), 'w', zipfile.ZIP_DEFLATED) as pkg:
  with open(os.path.join('manifest.json'), 'rb') as f:
    pkg.write('manifest.json')
  with open(os.path.join('icon.png'), 'rb') as f:
    pkg.write('icon.png')
  with open(os.path.join('CHANGELOG.md'), 'rb') as f:
    pkg.write('CHANGELOG.md')
  with open(os.path.join('README.md'), 'rb') as f:
    pkg.write('README.md')

  for assembly in ASSEMBLIES:
    with open(assembly, 'rb') as f:
      pkg.write(assembly, arcname=os.path.basename(assembly))

os.chdir(LAST_WORKDIR)
print('Done!')