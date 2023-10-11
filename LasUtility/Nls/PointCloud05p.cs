namespace LasUtility.Nls
{
    public class PointCloud05p
    {
        /// <summary>
        /// Point classifications for point clouds with density of 0.5 points / m2 used by National land survey of Finland (Maanmittauslaitos)
        /// </summary>
        public enum Classes
        {
            // https://www.maanmittauslaitos.fi/en/maps-and-spatial-data/expert-users/product-descriptions/laser-scanning-data-05-p
            // Accessed 27.9.2023

            // Data content: The input information of Laser scanning data 0.5 p is Laser scanning data 5 p, which has the following characteristics:

            // The point cloud has been quality checked and processed as well as possible to form the foundation
            // for the nationwide elevation model and to be suitable for the needs of the nationwide forest classification.
            // The classifications of point clouds have been implemented as automatic classifications.
            // The classifications of air points or error points mentioned below have mainly been done automatically,
            // and in practice they are never fully comprehensive. Other users of the data can filter and classify the data according to their own needs.

            // The point density (pulse density, i.e.outgoing laser pulses per square metre) is comprehensively at least 5 points/m²,
            // i.e.the distance between laser points on the ground is on average no more than approx. 0.40 m.
            // The distribution of points (scanning image) is not necessarily completely even,
            // but it depends on the type of scanner and the settings of each scanning flight.

            // Point classes:

            Unclassified = 1,   // Unclassified (class value 1 according to LAS 1.2 format, Unclassified). Before classification,
                                // all the laser points are in this class. After classification,
                                // this class includes all the points whose class has not changed in the classification process.


            Overlap = 12,       // Overlap area(class value 12 according to LAS 1.2 format, Overlap).
                                // In case of overlapping trajectories, further classification only includes points from one trajectory.
                                // The rest of the points are included in this class.
                                // These points used for combining trajectories have been deleted from the data to lighten the data load,
                                // but they have been stored for possible exceptional needs.

            Isolated = 16,      // Isolated, class value 16. Single points in the air and on the ground are classified
                                // in the Isolated class to decrease error points.A point is classified in this class,
                                // if there are 10 or fewer other points within a radius of 5 metres from the point.
                                // Some laser points from real features, such as points from power lines,
                                // or tree trunks in an open forest, are also included in this class.

            LowErrror = 7,      // Low error points(class value 7 according to LAS 1.2 format, Low Point).
                                // These points are according to the automatic classification statistically too low
                                // compared to the points in their surroundings.They can be due to e.g.scanner faults,
                                // multiple reflections of the laser pulse, or an incorrect separation of the return echo in the scanning system.

            Ground = 2,         // Ground (class value 2 according to LAS 1.2 format, Ground).
                                // These points represent the lowest surface that can be perceived from the air.
                                // The result depends on the values chosen for the parameters of the classification algorithm,
                                // and it is always a compromise between the number of points not belonging to the surface of the ground
                                // and points that the surface of the ground is lacking.

            Air = 15,           // Air points, class value 15. Clouds, flying objects or other objects in the air are classified in this class.

            Fault = 17,         // Fault points, class value 17. Points due to scanner faults,
                                // remaining after automatic classifications are classified in this class.

            // Remaining unclassified(default class) laser points are classified according to elevation level
            // in relation to the surface of the ground in three stages.The points include more than just vegetation points,
            // even if the name of the class refers to vegetation.

            LowVegetation = 3,  // Low vegetation (class value 3 according to LAS 1.2 format, Low Vegetation).
                                // Laser points from the height of 0.0–0.5 metres above ground level are classified in class 3.

            MedVegetation = 4,  // Medium vegetation(class value 4 according to LAS 1.2 format, Medium Vegetation).
                                // Laser points from the height of 0.5–2.0 metres above ground level are classified in class 4.

            HighVegetation = 5, // High vegetation(class value 5 according to LAS 1.2 format, High Vegetation).
                                // Laser points from the height of 2.0–50.0 metres above ground level are classified in class 5.

            // The point density of Laser scanning data 0.5 p has been spaced out from the original density of 5 p/m2 to 0.5 points/m2,
            // with the exception of limited areas pursuant to Section 14 of the Territorial Surveillance Act, where the spaced-out point density is 0.3 p/m².
            // Due to the spacing out, the horizontal distribution of points is as even as possible.The average horizontal distance
            // between points is approx. 1.4 m, in limited areas approx.1.8 m.

            // The point classes are otherwise the same as in the original Laser scanning data 5 p,
            // but all fault point classes have been deleted before the spacing out.

            // The spacing out has been done with Terra Solid's TerraScan programme,
            // so that points distributed as evenly as possible that correspond to the first return echoes
            // and other return echoes related to the corresponding input pulses are chosen.

            // Laser scanning data 5 p has been processed into blocks of 1 x 1 km(1/9 of a UTM 5,000 map sheet).
            // In Laser scanning data 0.5 p, these blocks have been combined in 3 x 3 km UTM 5,000 map sheets,
            // and the borders of the small blocks can be perceived at some viewing levels of the point cloud.
        }
    }

}

