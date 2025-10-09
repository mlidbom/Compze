# rules for slnx file and file system structure

- starting with the Compze solution folder, the slnx solution folder structure should be like this:
  - the solution-folder and project file name pattern should be: 
    - folder:Compze/A/B/C/
    - project-file: Compze.A.B.C.csproj
 - Compze/A/B/C/ may also contain one level down nested projects:
   - Compze.A.B.C.D.csproj
   - Compze.A.B.C.E.csproj
   
- starting with the /src/Compze folder, the file system structure to filename matching should ALWAYS be this
  - file-name: Compze.A.B.C.D.csproj 
  - directory: Compze/A/B/C/D

- Not every file system folder must have a csproj file. It is fine for it to just be a folder belonging to a csproj in a parent folder