import os
import sys
import platform
import argparse
import shutil
import re


def windows_paths():
  paths = []

  DRIVES = ["C:\\", "D:\\", "E:\\", "F:\\", "G:\\", "H:\\", "I:\\", "J:\\"]
  INSTALL_PATHS = [
    "Program Files (x86)\\Steam\\steamapps\\common\\",
    "Program Files\\Steam\\steamapps\\common\\",
    "SteamLibrary\\steamapps\\common\\"
  ]

  for drive in DRIVES:
    for install_path in INSTALL_PATHS:
      paths.append(drive + install_path + "ATLYSS\\")

  return paths

# Ensure script location is current directory
os.chdir(os.path.dirname(os.path.abspath(__file__)))

OS_TYPE = platform.system()
GAMEPATH_PROPS_PATH = 'GamePath.props'

if OS_TYPE == 'Windows':
  SEARCH_PATHS = windows_paths()
  GAME_EXECUTABLE = 'ATLYSS.exe'
else:
  print("This script doesn't support your platform yet, try manually specifying ATLYSS_PATH in _Utils/InstallPath.props.")
  sys.exit(1)

print(f'Searching for the game install in {len(SEARCH_PATHS)} paths.')

CHOSEN_PATH = None
for path in SEARCH_PATHS:
  if os.path.exists(os.path.join(path, GAME_EXECUTABLE)):
    CHOSEN_PATH = path
    break

if CHOSEN_PATH is None:
  print("Couldn't determine install path, try manually specifying ATLYSS_PATH in _Utils/InstallPath.props.")
  sys.exit(1)

with open('InstallPath.props', 'r') as f:
  PROPS_DATA = f.read()

match = re.search("<ATLYSS_PATH>(.*)</ATLYSS_PATH>", PROPS_DATA)

if match is None:
  print("Couldn't determine props location, try manually specifying ATLYSS_PATH in _Utils/InstallPath.props.")
  sys.exit(1)

PREVIOUS_PATH = match.group(1)

if not os.path.exists(os.path.join(PREVIOUS_PATH, GAME_EXECUTABLE)):
  print(f"Previous ATLYSS_PATH {PREVIOUS_PATH} is invalid!")
  print(f"Overriding ATLYSS_PATH value with {CHOSEN_PATH}")
else:
  print(f"Previous ATLYSS_PATH {PREVIOUS_PATH} seems valid, won't overwrite it.")
  print(f"The detected path was {CHOSEN_PATH} in case you need it.")
  sys.exit(0)
  

NEW_PATH = str.replace(fr"<ATLYSS_PATH>{CHOSEN_PATH}</ATLYSS_PATH>", "\\", "\\\\")

NEW_PROPS_DATA = re.sub("<ATLYSS_PATH>(.*)</ATLYSS_PATH>", NEW_PATH, PROPS_DATA)

with open('InstallPath.props', 'w') as f:
  f.write(NEW_PROPS_DATA)

print(f"Set ATLYSS_PATH to {CHOSEN_PATH}")