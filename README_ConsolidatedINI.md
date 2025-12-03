# Program Manager - Consolidated Profile INI System

## ?? **Updated Architecture: One INI Per Profile**
Your Program Manager now uses **consolidated INI files** - one file per profile containing all group settings!

## ?? **New File Structure**

### **Before: Multiple INI Files**
```
ProgramManagerVC.exe
??? Groups/
??? Office.ini          ? Separate file per group
??? Games.ini           ? Separate file per group  
??? Development.ini     ? Separate file per group
??? Main.ini            ? Separate file per group
??? Progman.ini         ? App settings
```

### **After: Consolidated Profile INIs**
```
ProgramManagerVC.exe
??? Groups/
??? Main.ini            ? All Main profile groups
??? TestProfile.ini     ? All TestProfile groups
??? WorkProfile.ini     ? All WorkProfile groups
??? Progman.ini         ? App & profile references
```

## ??? **Profile INI Structure**

### **Example: TestProfile.ini**
```ini
[Profile]
Name=TestProfile
Path=C:\TestFolders
Created=2025-01-03 10:30:00

[Group_Main]
Name=Main
Status=1
X=100
Y=100
Width=400
Height=300

[Group_Office]
Name=Office
Status=1
X=120
Y=120
Width=450
Height=350

[Group_Games]
Name=Games
Status=2
X=140
Y=140
Width=500
Height=400

[Group_Development]
Name=Development
Status=0
X=160
Y=160
Width=350
Height=250
```

### **Section Breakdown**

**`[Profile]` Section:**
- **Name** - Display name of the profile
- **Path** - Folder location containing groups
- **Created** - Timestamp when profile was created

**`[Group_GroupName]` Sections:**
- **Name** - Display name of the group window
- **Status** - Window state (0=Minimized, 1=Normal, 2=Maximized)
- **X, Y** - Window position coordinates
- **Width, Height** - Window dimensions

## ?? **Technical Implementation**

### **Profile Management**
```csharp
// Create profile ? Creates ProfileName.ini
FileBasedData.SaveProfile("WorkProfile", "C:\Work\Tools");

// Switch profile ? Loads from ProfileName.ini  
FileBasedData.SetCurrentProfile("WorkProfile");

// Delete profile ? Removes ProfileName.ini
FileBasedData.DeleteProfile("WorkProfile");
```

### **Group Settings**
```csharp
// Save group settings ? Updates [Group_GroupName] section
FileBasedData.SaveGroupSettings(groupInfo);

// Load group settings ? Reads from current profile INI
var groups = FileBasedData.GetAllGroups();
```

## ?? **Key Benefits**

### **Organization**
? **One file per profile** - Easy to identify and manage  
? **All group data consolidated** - Everything in one place  
? **Profile metadata included** - Name, path, creation date  
? **Reduced file clutter** - Fewer files in app directory  

### **Functionality**
? **Easy backup** - Copy one INI file per profile  
? **Profile sharing** - Send someone your TestProfile.ini  
? **Quick switching** - Profile data loads from one file  
? **Clear separation** - Each profile completely independent  

### **Maintenance**
? **Simpler debugging** - All settings in one location  
? **Easier migration** - Move one file per profile  
? **Better organization** - Logical grouping of related data  

## ?? **Migration Process**

**From Previous System:**
1. **Old system** had Office.ini, Games.ini, etc.
2. **New system** consolidates into ProfileName.ini
3. **Automatic conversion** when profile is loaded
4. **Old files** can be safely deleted

**Example Migration:**
```
OLD FILES:                NEW FILE:
Office.ini       ?       Main.ini
Games.ini                  [Group_Office]
Tools.ini        ?         [Group_Games]  
Main.ini                   [Group_Tools]
                          [Group_Main]
```

## ?? **Profile INI Locations**

**Default Profile:**
- **File:** `Main.ini`
- **Path:** Points to `ApplicationFolder/Groups/`
- **Auto-created** if missing

**Custom Profiles:**
- **File:** `ProfileName.ini` (e.g., `WorkProfile.ini`)
- **Path:** Points to user-selected folder
- **Created** when profile is added

## ?? **Testing the System**

1. **Launch Program Manager**
2. **Create multiple profiles** with different paths
3. **Move/resize group windows** in each profile
4. **Check app folder** - see consolidated INI files
5. **Switch profiles** - verify settings persist correctly

### **Expected Results**
- One INI file per profile in app directory
- All group settings consolidated within each file
- Profile switching loads correct window positions
- Clean, organized file structure

---
*Your Program Manager now provides consolidated, profile-based configuration management! ??*