csc /t:library /out:HaliteHelper.dll HaliteHelper.cs
csc /t:library /out:HaliteHelperSecondBot.dll HaliteHelperSecondBot.cs
csc /reference:HaliteHelper.dll -out:MyBot.exe MyBot.cs
csc /reference:HaliteHelperSecondBot.dll -out:SecondBot.exe MySecondBot.cs 
csc /reference:HaliteHelper.dll -out:CamBot.exe CamBot.cs
halite -d "30 30" "MyBot.exe" "SecondBot.exe" "CamBot.exe"
