# LasUtility

## Overview
LasUtility is a C# library designed to work with LIDAR data, providing a range of functionalities from handling LAS files to generating Digital Elevation Models (DEM).

## Features
- **Common**: Contains shared mathematical functions.
- **DEM**: Supports operations like triangulation.
- **LAS**: Functionality for for reading and writing LIDAR data.
- **Nls**: Tool for extracting the bounding box coordinates from map tile names used by the National Land Survey of Finland.
- **Shapefile**: Tools for rasterising Esri Shapefiles.
- **VoxelGrid**: Holds the point cloud data in a 3D grid for fast access.

## License
The library is available under MIT license. Note that this library depends on some 3rd party packages that have their own lisencing models.

### 3rd party libraries
 - [LASZip](https://github.com/LASzip/LASzip), [Apache-2.0 License](http://www.apache.org/licenses/LICENSE-2.0)
 - [LasZipNetStandard](https://github.com/Kuoste/LasZipNetStandard), [Apache-2.0 License](http://www.apache.org/licenses/LICENSE-2.0)
 - [MessagePack](https://github.com/MessagePack-CSharp/MessagePack-CSharp), [MIT License](https://en.wikipedia.org/wiki/MIT_license)
 - [MIConvexHull](https://github.com/DesignEngrLab/MIConvexHull), [MIT License](https://en.wikipedia.org/wiki/MIT_license)
 - [NetTopologySuite](https://github.com/NetTopologySuite/NetTopologySuite), [BSD-3-Clause](https://licenses.nuget.org/BSD-3-Clause)
	


