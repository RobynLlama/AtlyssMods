WORKDIR=$(pwd)
APPPATH="D:/SteamLibrary/steamapps/common/ATLYSS"
DLLPATH="${APPPATH}/ATLYSS_Data/Managed"
WORKSPACE="${WORKDIR}/AtlyssWorkspace"
OUTPUTPATH="${WORKDIR}/AtlyssBepInEx"
HOOKGENPATH="${WORKDIR}/MonoMod-22.07.31.01-net452"
BEPINEXPATH="${WORKDIR}/BepInEx_win_x64_5.4.23.2"

# Make sure workspace and output folders exist, and that output is clear
mkdir $WORKSPACE
mkdir $OUTPUTPATH
rm $OUTPUTPATH/* -rf

# Copy over Atlyss's assembly
cp $DLLPATH/Assembly-CSharp.dll $WORKSPACE

# Generate hooks and publicized assembly
cd $HOOKGENPATH
./MonoMod.RuntimeDetour.HookGen.exe --private $WORKSPACE/Assembly-CSharp.dll
cd $WORKDIR

# Publicize game assembly
assembly-publicizer.exe  --output $WORKSPACE/PUBLIC-Assembly-CSharp.dll $WORKSPACE/Assembly-CSharp.dll

# Copy over BepInEx to output
cp $BEPINEXPATH/* $OUTPUTPATH -r

# Copy over hooks to plugins
mkdir $OUTPUTPATH/BepInEx/plugins
mkdir $OUTPUTPATH/BepInEx/utils
cp $WORKSPACE/MMHOOK_Assembly-CSharp.dll $OUTPUTPATH/BepInEx/plugins
cp $WORKSPACE/PUBLIC-Assembly-CSharp.dll $OUTPUTPATH/BepInEx/utils

# Copy Output stuff to game folder
rm $APPPATH/BepInEx -rf
cp $OUTPUTPATH/* $APPPATH -r