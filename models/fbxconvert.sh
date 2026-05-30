for filename in *.fbx
do
echo ${filename}
/e/SteamLibrary/steamapps/common/Blender/blender.exe --background --python fbxconvert.py -- ${filename}
done
