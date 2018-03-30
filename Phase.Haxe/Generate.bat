@echo off
set GeneratorDir=..\Tools\Phase.ExternGenerator\

echo Generating Std
neko %GeneratorDir%bin\Phase.ExternGenerator.n -i %GeneratorDir%api.haxe.org\xml\3.4.7\js.xml -o generated\std -p js --exclude js.* -tpkg haxe.root

echo Generating JavaScript
rem neko %GeneratorDir%bin\Phase.ExternGenerator.n -i %GeneratorDir%api.haxe.org\xml\3.4.7\js.xml -o generated\js -p js --include js.* -ns Haxe