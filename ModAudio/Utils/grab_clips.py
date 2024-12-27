import os
import glob
import json
from pathlib import Path

FOLDER_PATH = 'D:\\UnityProjects\\ATLYSS\\Assets\\ATLYSS\\Resources\\_sound'

def remap_old(data: str):
  data = data.removesuffix('.ogg').removesuffix('.wav').removesuffix('.mp3')
  data = '_'.join([x[0].upper() + x[1:] for xxxx in data.replace('(', '_').replace(')', '_').strip('_').split('\\') for xxx in xxxx.split(' ') for xx in xxx.split('-') for x in xx.split('_') if x != ''])
  return data

def remap(data: str):
  data = Path(data).stem
  data = '_'.join([x[0].upper() + x[1:] for xxxx in data.replace('(', '_').replace(')', '_').strip('_').split('\\') for xxx in xxxx.split(' ') for xx in xxx.split('-') for x in xx.split('_') if x != ''])
  return data

files = []
files += glob.glob("**\\*.ogg", root_dir=FOLDER_PATH, recursive=True)
files += glob.glob("**\\*.wav", root_dir=FOLDER_PATH, recursive=True)
files += glob.glob("**\\*.mp3", root_dir=FOLDER_PATH, recursive=True)

ids = [remap(x) for x in files]
clip_names = [Path(x).stem for x in files]
pairs = [x for x in zip(ids, clip_names)]
pairs.sort(key=lambda x: x[0])

if False:
  with open('data.json', 'w') as f:
    f.writelines([x + ' = ' + y + os.linesep for x, y in pairs])
else:
  with open('data.cs', 'w') as f:
    f.write('public const int TotalClips = ' + str(len(pairs)) + ';\n')
    f.writelines(['public const string ' + x + ' = \"' + y + '\";\n' for x, y in pairs])