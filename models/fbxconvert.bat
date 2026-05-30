for filename in *.fbx
do
echo ${filename}
/mnt/E69ABEB29ABE7F1D/LinuxSteam/SteamLibrary/steamapps/common/Blender/blender --background --python fbxconvert.py -- ${filename}
done
