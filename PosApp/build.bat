dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish
echo Now open pos.iss in Inno Setup and press Compile -> Output\PosSetup.exe
