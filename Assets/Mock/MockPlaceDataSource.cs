public static class MockPlaceDataSource
{
    public static PlaceList LoadPlaces()
    {
        return new PlaceList
        {
            places = new[]
            {
                new PlaceData
                {
                    name = "Tokyo",
                    longitude = 139.6917,
                    latitude = 35.6895,
                    height = 1000
                },
                new PlaceData
                {
                    name = "Bali",
                    longitude = 115.1889,
                    latitude = -8.4095,
                    height = 1000
                }
            }
        };
    }
}
