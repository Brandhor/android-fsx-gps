package brandon.fsx;

import android.app.Activity;
import android.content.Context;
import android.content.Intent;
import android.content.SharedPreferences;
import android.graphics.Color;
import android.graphics.drawable.Drawable;
import android.os.AsyncTask;
import android.os.Bundle;
import android.os.Handler;
import android.os.Looper;
import android.os.PowerManager;
import android.preference.PreferenceManager;
import android.util.Log;
import android.view.KeyEvent;
import android.view.Menu;
import android.view.MenuInflater;
import android.view.MenuItem;
import android.view.View;
import android.widget.TextView;
import android.widget.Toast;

import java.io.BufferedReader;
import java.io.IOException;
import java.io.InputStreamReader;
import java.net.URI;
import java.util.ArrayList;
import java.util.Collection;
import java.util.Iterator;
import java.util.List;
import java.util.Timer;
import java.util.TimerTask;

import org.apache.http.HttpResponse;
import org.apache.http.client.ClientProtocolException;
import org.apache.http.client.HttpClient;
import org.apache.http.client.methods.HttpGet;
import org.apache.http.impl.client.DefaultHttpClient;
import org.json.JSONArray;
import org.json.JSONException;
import org.json.JSONObject;
import org.json.JSONTokener;

import com.google.android.maps.GeoPoint;
import com.google.android.maps.MapActivity;
import com.google.android.maps.MapView;
import com.google.android.maps.MapController;
import com.google.android.maps.Overlay;
import com.google.android.maps.OverlayItem;

public class fsx extends MapActivity {
	List<Overlay> mapOverlays;
	Drawable userDrawable;
	Drawable othersDrawable;
	RotatedMapView map;
	private Timer myTimer;
	private Timer routeTimer;
	SharedPreferences preferences;
	private Handler handler = new Handler();
	private PowerManager.WakeLock wl;

	@Override
	protected void onPause() {
		super.onPause();
		wl.release();
		myTimer.cancel();
		routeTimer.cancel();
	}

	@Override
	protected void onResume() {
		super.onResume();
		wl.acquire();
		scheduleTimer();
	}


	/** Called when the activity is first created. */
	@Override
	public void onCreate(Bundle savedInstanceState) {
		super.onCreate(savedInstanceState);
		setContentView(R.layout.main);

		PowerManager pm = (PowerManager) getSystemService(Context.POWER_SERVICE);
		wl = pm.newWakeLock(PowerManager.FULL_WAKE_LOCK, "DoNotDimScreen");

		this.map = (RotatedMapView) findViewById(R.id.mapview);
		this.map.setBuiltInZoomControls(true);
		mapOverlays = this.map.getOverlays();
		if(map.isSatellite()) {
			userDrawable = this.getResources().getDrawable(R.drawable.planew);
		} else {
			userDrawable = this.getResources().getDrawable(R.drawable.plane);
		}

		othersDrawable = this.getResources().getDrawable(R.drawable.others_plane);

		preferences = PreferenceManager.getDefaultSharedPreferences(this);

		scheduleTimer();
	}

	private void scheduleTimer() {
		myTimer =  new Timer();
		myTimer.schedule(new TimerTask() {
			private Runnable runnable = new Runnable() {
				public void run() {
					new MapUpdateTask().execute();
				}
			};

			@Override
			public void run() {
				handler.post(runnable);
			}
		}, 0, 1000);

		routeTimer =  new Timer();
		routeTimer.schedule(new TimerTask() {
			private Runnable runnable = new Runnable() {
				public void run() {
					new RouteUpdateTask().execute();
				}
			};

			@Override
			public void run() {
				handler.post(runnable);
			}
		}, 0, 10000);
	}

	@Override
	protected boolean isRouteDisplayed() {
		return false;
	}

	@Override
	public boolean onCreateOptionsMenu(Menu menu) {
		MenuInflater inflater = getMenuInflater();
		inflater.inflate(R.menu.main_menu, menu);
		return true;
	}

	@Override
	public boolean onOptionsItemSelected(MenuItem item) {
		// Handle item selection
		switch (item.getItemId()) {
		case R.id.change_display_mode:
			if(map.isSatellite()) {
				map.setSatellite(false);
				userDrawable = this.getResources().getDrawable(R.drawable.plane);
			} else {
				map.setSatellite(true);
				userDrawable = this.getResources().getDrawable(R.drawable.planew);
			}
			return true;
			/*case R.id.show_route:
	    	Intent i = new Intent(fsx.this, Routeactivity.class);
	    	startActivity(i);
	    	return true;*/
		case R.id.settings:
			Intent i1 = new Intent(fsx.this, Preferences.class);
			startActivity(i1);
			return true;
		case R.id.quit:
			finish();
		default:
			return super.onOptionsItemSelected(item);
		}
	}

	public static String getHttpPage(String uri)
	{
		BufferedReader in = null;
		try {
			HttpClient httpclient = new DefaultHttpClient();

			HttpGet request = new HttpGet(uri);
			HttpResponse response;
			try {
				response = httpclient.execute(request);
				in = new BufferedReader
				(new InputStreamReader(response.getEntity().getContent()));
				StringBuffer sb = new StringBuffer("");
				String line = "";
				String NL = System.getProperty("line.separator");
				while ((line = in.readLine()) != null) {
					sb.append(line + NL);
				}
				in.close();

				String page = sb.toString();
				return page;
			} catch (ClientProtocolException e) {
				// TODO Auto-generated catch block
				e.printStackTrace();
			} catch (IOException e) {
				// TODO Auto-generated catch block
				e.printStackTrace();
			}
		} finally {
			if (in != null) {
				try {
					in.close();
				} catch (IOException e) {
					e.printStackTrace();
				}
			}
		}
		return null;
	}

	private class MapUpdateTask  extends AsyncTask<Void, Void, String> {
		@Override
		protected String doInBackground(Void... arg0) {
			String ip = preferences.getString("ipaddress", "192.168.2.22");
			String port = preferences.getString("port", "40000");

			String uri = "http://"+ip+":"+port+"/";
			//String uri = "http://192.168.2.22/fsx.php";
			return getHttpPage(uri);
		}

		protected void onPostExecute(String page) {
			if(page == null) {
				return;
			}

			Collection overlaysToAddAgain = new ArrayList();
			for (Iterator iter = map.getOverlays().iterator(); iter.hasNext();) {
				Object o = iter.next();
				if (!PlaneOverlay.class.getName().equals(o.getClass().getName())) {
					overlaysToAddAgain.add(o);
				}
			}
			map.getOverlays().clear();
			map.getOverlays().addAll(overlaysToAddAgain);

			JSONObject object;
			try {
				object = (JSONObject) new JSONTokener(page).nextValue();
				JSONArray planes = object.getJSONArray("array");
				for (int i = 0; i < planes.length(); i++) {
					JSONObject obj = planes.getJSONObject(i);
					String title = obj.getString("title");
					Double latitude = obj.getDouble("latitude");
					Double longitude = obj.getDouble("longitude");
					Double altitude = obj.getDouble("altitude");
					Float heading = (float)obj.getDouble("heading");
					Boolean isUser = obj.getBoolean("isUser");

					GeoPoint c = new GeoPoint((int)(latitude*1E6), (int)(longitude*1E6));

					if(isUser) {
						setTitle(title);

						MapController mc = map.getController();
						if(preferences.getBoolean("mapcentering", true))
							mc.animateTo(c);

						if(preferences.getBoolean("maprotation", false)) {
							map.setHeading(heading);
						} else {
							map.setHeading(0.0f);
						}

						mapOverlays.add(new PlaneOverlay(userDrawable, c, heading));
					} else {

						mapOverlays.add(new PlaneOverlay(othersDrawable, c, heading));
					}
				}
			} catch (JSONException e) {
				// TODO Auto-generated catch block
				e.printStackTrace();
			} catch (ClassCastException e) {
				e.printStackTrace();
			}
		}
	};

	private class RouteUpdateTask  extends AsyncTask<Void, Void, String> {
		@Override
		protected String doInBackground(Void... arg0) {
			String ip = preferences.getString("ipaddress", "192.168.2.22");
			String port = preferences.getString("port", "40000");

			String uri = "http://"+ip+":"+port+"/route/";
			//String uri = "http://192.168.2.22/route.php";
			return getHttpPage(uri);
		}

		protected void onPostExecute(String page) {
			if(page == null) {
				return;
			}
			int color = Color.parseColor("#f70000");

			Collection overlaysToAddAgain = new ArrayList();
			for (Iterator iter = map.getOverlays().iterator(); iter.hasNext();) {
				Object o = iter.next();
				if (!RouteOverlay.class.getName().equals(o.getClass().getName())) {
					overlaysToAddAgain.add(o);
				}
			}
			map.getOverlays().clear();
			map.getOverlays().addAll(overlaysToAddAgain);

			String path = page.trim();
			//String path = "41.126001,16.868379 41.895466,12.482324 45.438367,10.991748";
			if (path != null && path.length() > 0) {
				String[] pairs = path.split("\n");
				String[] lngLat = pairs[0].split(",");

				try {
					GeoPoint startGP = new GeoPoint((int) (Double.parseDouble(lngLat[0]) * 1E6), (int) (Double.parseDouble(lngLat[1]) * 1E6));
					map.getOverlays().add(new RouteOverlay(startGP, startGP, 1));
					GeoPoint gp1;
					GeoPoint gp2 = startGP;

					for (int i = 1; i < pairs.length; i++)
					{
						lngLat = pairs[i].split(",");

						gp1 = gp2;

							gp2 = new GeoPoint((int) (Double.parseDouble(lngLat[0]) * 1E6), (int) (Double.parseDouble(lngLat[1]) * 1E6));

							map.getOverlays().add(new RouteOverlay(gp1, gp2, 3, color));
					}
					map.getOverlays().add(new RouteOverlay(gp2, gp2, 3));
				} catch (NumberFormatException e) {
				}
			}
		}
	};
}