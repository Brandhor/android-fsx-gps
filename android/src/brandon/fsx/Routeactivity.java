package brandon.fsx;

import java.util.ArrayList;
import java.util.Collection;
import java.util.Iterator;

import android.content.Context;
import android.graphics.Color;
import android.os.Bundle;
import android.os.PowerManager;
import android.preference.PreferenceManager;
import android.util.Log;

import com.google.android.maps.GeoPoint;
import com.google.android.maps.MapActivity;
import com.google.android.maps.MapView;

public class Routeactivity  extends MapActivity {
	
	MapView mapView;
	
	@Override
	public void onCreate(Bundle savedInstanceState) {
		super.onCreate(savedInstanceState);
		setContentView(R.layout.route);
		
		this.mapView = (MapView) findViewById(R.id.routemapview);
		drawPath(Color.parseColor("#f70000"), this.mapView);

	}
	
	@Override
	protected boolean isRouteDisplayed() {
		return true;
	}
	
	public void drawPath(int color, MapView mMapView01) {

		// color correction for dining, make it darker
		//if (color == Color.parseColor("#add331")) color = Color.parseColor("#6C8715");

		Collection overlaysToAddAgain = new ArrayList();
		for (Iterator iter = mMapView01.getOverlays().iterator(); iter.hasNext();) {
			Object o = iter.next();
			if (!RouteOverlay.class.getName().equals(o.getClass().getName())) {
				// mMapView01.getOverlays().remove(o);
				overlaysToAddAgain.add(o);
			}
		}
		mMapView01.getOverlays().clear();
		mMapView01.getOverlays().addAll(overlaysToAddAgain);

		//String path = navSet.getRoutePlacemark().getCoordinates();
		String path = "41.126001,16.868379 41.895466,12.482324 45.438367,10.991748";
		if (path != null && path.trim().length() > 0) {
			String[] pairs = path.trim().split(" ");


			String[] lngLat = pairs[0].split(","); // lngLat[0]=longitude lngLat[1]=latitude lngLat[2]=height


			//if (lngLat.length<3) lngLat = pairs[1].split(","); // if first pair is not transferred completely, take seconds pair //TODO 

			try {
				GeoPoint startGP = new GeoPoint((int) (Double.parseDouble(lngLat[0]) * 1E6), (int) (Double.parseDouble(lngLat[1]) * 1E6));
				mMapView01.getOverlays().add(new RouteOverlay(startGP, startGP, 1));
				GeoPoint gp1;
				GeoPoint gp2 = startGP;

				for (int i = 1; i < pairs.length; i++) // the last one would be crash
				{
					lngLat = pairs[i].split(",");

					gp1 = gp2;

					if (lngLat.length >= 2 && gp1.getLatitudeE6() > 0 && gp1.getLongitudeE6() > 0
							&& gp2.getLatitudeE6() > 0 && gp2.getLongitudeE6() > 0) {

						// for GeoPoint, first:latitude, second:longitude
						gp2 = new GeoPoint((int) (Double.parseDouble(lngLat[0]) * 1E6), (int) (Double.parseDouble(lngLat[1]) * 1E6));

						if (gp2.getLatitudeE6() != 22200000) { 
							mMapView01.getOverlays().add(new RouteOverlay(gp1, gp2, 3, color));
						}
					}
					// Log.d(myapp.APP,"pair:" + pairs[i]);
				}
				//routeOverlays.add(new RouteOverlay(gp2,gp2, 3));
				mMapView01.getOverlays().add(new RouteOverlay(gp2, gp2, 3));
			} catch (NumberFormatException e) {
			}
		}
		// mMapView01.getOverlays().addAll(routeOverlays); // use the default color
		mMapView01.setEnabled(true);
	}


}
