// file: match.java
//

// import necessary java libraries
//
import javax.swing.*;
import java.awt.*;
import java.awt.event.*;
import java.lang.*;
import java.lang.Double;
import java.io.*;
import java.net.*;
import java.util.*;
import java.text.*;
import java.applet.AudioClip;

/**
 * match is the search engine of the recognizer
 */
public class match implements Constants{
    
    // the data array to store all of the templates
    private lpcData[] template;
    // the data to compute the matching score
    private lpcData signal, reference;
    // the url of lpc signal
    private URL lpcURL = null;
    // the url of raw signal
    private URL rawURL = null;
    // the number of templates
    private int num_of_templates = 11;

    // the index of template for best match method
    private int templateIndex;

    // recognize result, index to show the template
    public int recogResult;

    // the slope constraint value
    private double slope = 0.0;

    // the distance type between frames
    private String distanceType = "Itakura";

    // the applet that this program will be run in
    private dtwApplet applet;

    // declare the length of path
    private int pathLength[];

    /**
     * array to store the best path for each template
     */
    public int pathRecord[][];

    /**
     * array to store the associated cost for each template
     */
    public double costRecord[];

    /**
     * array to store the total cost for the best path of each template
     */
    public double totalCost[];

    /**
     * url to store the audio file for the test signal
     */
    private URL audiofile = null;

    // mainmenu that controls this process
    private MainMenu control;

    // **********************************************************************
    //
    // constructor method
    //
    // **********************************************************************

    /**
     * default class constructor
     *
     * @param dtwapp applet object instantiated
     */
    public match(dtwApplet dtwapp) {
	applet = dtwapp;
	templateIndex = -1;
	recogResult = -1;

	pathRecord = new int[num_of_templates][];
	costRecord = new double[120];
	pathLength = new int[num_of_templates];
	totalCost = new double[num_of_templates];

	signal = null;
	reference = null;
    }

    // ******************************************************************
    //
    // class methods
    //
    // ******************************************************************

    /**
     * method to load in the templates
     *
     * @throws MalformedURLException if applet codebase cannot be initilized
     */
    public void initTemplates() {
	int i, j, k;
	URL url[];
	URL templateurl[]  = new URL[num_of_templates];
	
	try {
	    templateurl[0] = new URL(applet.getCodeBase(), 
				     "data/templates/one_model.text");
	    templateurl[1] = new URL(applet.getCodeBase(), 
				     "data/templates/two_model.text");
	    templateurl[2] = new URL(applet.getCodeBase(), 
				     "data/templates/three_model.text");
	    templateurl[3] = new URL(applet.getCodeBase(), 
				     "data/templates/four_model.text");
	    templateurl[4] = new URL(applet.getCodeBase(), 
				     "data/templates/five_model.text");
	    templateurl[5] = new URL(applet.getCodeBase(), 
				     "data/templates/six_model.text");
	    templateurl[6] = new URL(applet.getCodeBase(), 
				     "data/templates/seven_model.text");
	    templateurl[7] = new URL(applet.getCodeBase(), 
				     "data/templates/eight_model.text");
	    templateurl[8] = new URL(applet.getCodeBase(), 
				     "data/templates/nine_model.text");
	    templateurl[9] = new URL(applet.getCodeBase(), 
				     "data/templates/zero_model.text");
	    templateurl[10] = new URL(applet.getCodeBase(), 
				      "data/templates/oh_model.text");
	}
	catch (MalformedURLException e) {
	    System.out.println(e);
	    control.setErrorInfo("Error opening template");
	    return;
	}
	template = new lpcData[num_of_templates];

	for (i=0; i<num_of_templates; i++) {
	    template[i] = new lpcData(templateurl[i]);
	    if (template[i].getLength() == 0) {
		control.setErrorInfo("Error opening template. \n Check template URL and restart the applet later.");
		template[0] = null;
		return;
	    }
	}
    }

    /**
     * access method to set slope
     *
     * @param a double value of slope to be set
     */
    public void setSlope(double a) {
	slope = a;
    }

    /**
     * access method to get slope
     *
     * @return slope value
     */
    public double getSlope() {
	return slope;
    }

    /**
     * access method for setting distance type
     *
     * @param a String value of distance type to be set
     */
    public void setDistance(String a) {
	distanceType = a;
    }

    /**
     * access method for getting distance type
     *
     * @return distance type value
     */
    public String getDistanceType() {
	return distanceType;
    }

    /**
     * access method for getting length of the best path
     *
     * @return best path length
     */
    public int bestPathLength() {
	return pathLength[recogResult];
    }

    /**
     * access method for set reference index
     *
     * @param a index to set
     */
    public void setRefIndex(int a) {
        templateIndex = a;
	recogResult = a;
    }

    /**
     * access method for getting signal 
     *
     * @return current signal
     */
    public lpcData getSignal() {
	return signal;
    }

    /**
     * access method for getting lpc URL
     *
     * @return URL for the lpc
     */
    public URL getLpcURL() {
	return lpcURL;
    }

    /**
     * access method for getting the raw URL
     *
     * @return raw URL
     */
    public URL getRawURL() {
	return rawURL;
    }

    /**
     * access method for getting URL of audio file
     *
     * @return URL of audio file
     */
    public URL getAudioFile() {
	return audiofile;
    }

    /**
     * access method for setting URL of audio file
     *
     * @param a URL to set
     */
    public void setAudioFile(URL a) {
	audiofile = a;
    }

    /**
     * access method for getting the lpcData reference
     *
     * @return reference of lpcData type
     */
    public lpcData getReference() {
	return reference;
    }

    /**
     * access method for setting the reference with URL
     *
     * @param url URL of reference to set
     */
    public void setReference(URL url) {
	if(url !=null) {
	    reference = new lpcData(url);
	    if (reference.getLength() == 0) {
		control.setErrorInfo("Error opening template");
		reference = null;
	    }
	}
	else 
	    reference = null;
    }

    /**
     * access method for setting the reference with an index
     *
     * @param k integer value of index to template array
     */
    public void setReference(int k) {
	reference = template[k];
	if ((reference == null) || (reference.getLength() == 0)) {
	    control.setErrorInfo("Error opening template" + k);
	    reference = null;
	}
    }

    /**
     * access method for setting the signal with a URL
     *
     * @param url URL of signal
     */
    public void setSignal(URL url) {
	if(url != null) {
	    signal = new lpcData(url); 
	    if (signal.getLength() == 0) {
		control.setErrorInfo("Error opening signal");
		signal = null;
		return;
	    }
	}
	else 
	    signal = null;
    }

    /**
     * correct the datapath to match each case of speech sound
     *
     * @param k index of signal to set
     */
    public void setSignal(int k) {
	
	try{
	    switch (k) {
	    case 0: 
		lpcURL = new URL(applet.getCodeBase(), "../data/lpc/man/ae/ae_1b.sof");
		rawURL = new URL(applet.getCodeBase(), "../data/raw8k/ae/ae_1b.raw");
		audiofile = new URL(applet.getCodeBase(), "../data/audiofiles/man/ae/ae_1b.au");
		break;
	    case 1:
		lpcURL = new URL(applet.getCodeBase(), "../data/lpc/man/ae/ae_2b.sof");
		rawURL = new URL(applet.getCodeBase(), "../data/raw8k/ae/ae_2b.raw");
		audiofile = new URL(applet.getCodeBase(), "../data/audiofiles/man/ae/ae_2b.au");
		break;
	    case 2:
		lpcURL = new URL(applet.getCodeBase(), "../data/lpc/man/ae/ae_3b.sof");
		rawURL = new URL(applet.getCodeBase(), "../data/raw8k/ae/ae_3b.raw");
		audiofile = new URL(applet.getCodeBase(), "../data/audiofiles/man/ae/ae_3b.au");
		break;
	    case 3:
		lpcURL = new URL(applet.getCodeBase(), "../data/lpc/man/ae/ae_4b.sof");
		rawURL = new URL(applet.getCodeBase(), "../data/raw8k/ae/ae_4b.raw");
		audiofile = new URL(applet.getCodeBase(), "../data/audiofiles/man/ae/ae_4b.au");
		break;
	    case 4:
		lpcURL = new URL(applet.getCodeBase(), "../data/lpc/man/ae/ae_5b.sof");
		rawURL = new URL(applet.getCodeBase(), "../data/raw8k/ae/ae_5b.raw");
		audiofile = new URL(applet.getCodeBase(), "../data/audiofiles/man/ae/ae_5b.au");
		break;
	    case 5:
		lpcURL = new URL(applet.getCodeBase(), "../data/lpc/man/ae/ae_6b.sof");
		rawURL = new URL(applet.getCodeBase(), "../data/raw8k/ae/ae_6b.raw");
		audiofile = new URL(applet.getCodeBase(), "../data/audiofiles/man/ae/ae_6b.au");
		break;
	    case 6:
		lpcURL = new URL(applet.getCodeBase(), "../data/lpc/man/ae/ae_7b.sof");
		rawURL = new URL(applet.getCodeBase(), "../data/raw8k/ae/ae_7b.raw");
		audiofile = new URL(applet.getCodeBase(), "../data/audiofiles/man/ae/ae_7b.au");
		break;
	    case 7:
		lpcURL = new URL(applet.getCodeBase(), "../data/lpc/man/ae/ae_8b.sof");
		rawURL = new URL(applet.getCodeBase(), "../data/raw8k/ae/ae_8b.raw");
		audiofile = new URL(applet.getCodeBase(), "../data/audiofiles/man/ae/ae_8b.au");
		break;
	    case 8:
		lpcURL = new URL(applet.getCodeBase(), "../data/lpc/man/ae/ae_9b.sof");
		rawURL = new URL(applet.getCodeBase(), "../data/raw8k/ae/ae_9b.raw");
		audiofile = new URL(applet.getCodeBase(), "../data/audiofiles/man/ae/ae_9b.au");
		break;
	    case 9:
		lpcURL = new URL(applet.getCodeBase(), "../data/lpc/man/ae/ae_zb.sof");
		rawURL = new URL(applet.getCodeBase(), "../data/raw8k/ae/ae_zb.raw");
		audiofile = new URL(applet.getCodeBase(), "../data/audiofiles/man/ae/ae_zb.au");
		break;
	    case 10:
		lpcURL = new URL(applet.getCodeBase(), "../data/lpc/man/ae/ae_ob.sof");
		rawURL = new URL(applet.getCodeBase(), "../data/raw8k/ae/ae_ob.raw");
		audiofile = new URL(applet.getCodeBase(), "../data/audiofiles/man/ae/ae_ob.au");
		break;
	    }
	} catch (MalformedURLException e) {
	    control.setErrorInfo("Error opening signal");
	    signal = null;
	    return;
	}
	if(lpcURL != null) {
	    signal = new lpcData(lpcURL);
	    if (signal.getLength() == 0) {
		control.setErrorInfo("Error opening signal");
		signal = null;
		return;
	    }
	}
	    
    }

    // methods to compute distance between two frames    
    private double frameDistance(double f1[], double f2[]) {
	double dis = 0.0;
	if (distanceType == "Euclidean") 
	    dis = EuclideanDistance(f1, f2);
	else if (distanceType == "Absolute") 
	    dis = AbsDistance(f1, f2);
	else if (distanceType == "Itakura")
	    dis = ITDDistance(f1, f2);
	return dis;
    }

    // method to compute the Euclidean distance between two vectors
    private double EuclideanDistance(double frame1[], double frame2[]) {
	int i;
	double dis = 0.0;

	for (i=0; i<13; i++) {
	    dis = dis + (frame1[i] - frame2[i]) * (frame1[i] - frame2[i]);
	}
	return dis;
    }

    // method to compute the absolute distance between two vectors
    private double AbsDistance(double frame1[], double frame2[]) {
	int i;
	double dis = 0.0;

	for (i=0; i<13; i++) {
	    dis = dis + Math.abs(frame1[i] - frame2[i]);
	}
	return dis;
    }

    // method to compute the Itakura distance between two vectors
    private double ITDDistance(double ar2[], double  ar1[]) {
	double m2[]=new double[13]; 
	double rf[]=new double[13];
	double rf1[]=new double[13];
	double k,d;
	int i,j;


	for(i=0;i<13;i++) {
	    m2[i]=0;
	    rf[i]=ar1[i];
	}

	//autocorrelation of ar2 (lpcar2ra)
	for(i=0;i<13;i++) {
	    for(j=0; j<13-i; j++)
		m2[i] += ar2[j] * ar2[i+j];
	}

	//reflection coefficients from ar1 (lpcar2rf)
	for(j=11;j>0;j--) {
	    k = rf[j+1];
	    d = 1.0/(1.0-k*k);
	    for(i=1;i<=j;i++) {
		rf1[i] = (rf[i] - k * rf[j-i+1]) * d;
	    }
	    for(i=1;i<=j;i++) 
		rf[i]=rf1[i];
	}

	// autocorrelation coefs from rf (lpcrf2rr)
	double rr[] = new double[13];
	double a[] = new double[13];
	double sum;
	for(i=0;i<13;i++) {
	    rr[i] = 0.0;
	    a[i] = 0.0;
	}
	a[0] = rf[1];
	rr[0] = 1.0;
	rr[1] = - a[0];
	double e = a[0]*a[0] - 1.0;

	for(i=1;i<12;i++) {
	    k = rf[i+1];
	    sum = 0.0;
	    for(j=i;j>=1;j--)
		sum += rr[j]*a[i-j];

	    rr[i+1] = k*e - sum;

	    double aa[] = new double[13];
	    for(j=0;j<i;j++) 
		aa[j] = a[j] + k*a[i-j-1];
	    for(j=0;j<i;j++)
		a[j] = aa[j];
	    a[i] = k;

	    e = e*(1.0 - k*k);
	}

	double ar[] = new double[13];
	ar[0] = 1.0;
	for(i=0;i<12;i++)
	    ar[i+1]=a[i];

	sum = 0.0;
	for(i=0;i<13;i++) 
	    sum += rr[i]*ar[i];

	double r0 = 1.0 / sum;

	for(i=0;i<13;i++)
	    rr[i] *= r0;

	m2[0] *= 0.5;
	sum = 0.0;
	for(i=0;i<13;i++)
	    sum += rr[i]*m2[i];
	sum *= 2;
	sum = Math.log(sum);

	if(Math.abs(sum) < 1e-6) return 0.0;
	return sum;
    }


    /**
     * the major method to compute the matching score between selected test 
     * signal and reference. 
     *
     * @return if success, return true, else return false
     */
    public boolean lefttorightMatch() {
	if ((signal == null ) || (reference == null)) {
	    return false;
	}

	int I, J;
	int i, j, k, n;
	
	double cost[][];
	int path[][];
	int maxJ;
	int minJ;
	int tempJ;
	double tempcost;
	double temp = 0.0;
	int mink = -1;
	double minc = 1000000.0;

	// I is the length of test signal, and J is the length of reference
	I = signal.getLength();
	J = reference.getLength();
	//	System.out.println("Length of signal: " + I);
	//	System.out.println("Length of referece: " + J);

	path = new int[I][J];
	cost = new double[I][J];
	// initiate the path and cost array
	// -1 is to indicate that this element is not computed yet
	for (i=0; i<I; i++) {
	    for (j=0; j<J; j++) {
		path[i][j] = -1;
		cost[i][j] = -1.0;
	    }
	}
	
	// the starting point is (0, 0)
	cost[0][0] = frameDistance(reference.data[0], signal.data[0]);

	for (i=1; i<I; i++) {
	    // calculate the legal search region defined by slope
	    //	    System.out.println("Temp output for column " + i);
	    if (slope == 0) {
		maxJ = J-1;
		minJ = 0;
	    } else {
		maxJ = (int)(slope*i)+1;
		tempJ = (int)((J-1)*i*(1/slope)/I + (J-1)*(1-1/slope))+1;

		if (tempJ < maxJ) {
		    maxJ = tempJ;
		}
		if (maxJ >= J) {
		    maxJ = J-1;
		}

		minJ = (int)((J-1)*i/((I-1)*slope))-1;
		tempJ = (int)(J-1-(I-1-i)*slope)-1;
		if (tempJ > minJ ) {
		    minJ = tempJ;
		}
		if (minJ < 0) {
		    minJ = 0;
		}
	    }

	    // for each legal node, search the predecessor with least cost
	    //	    System.out.println("search area: maxJ=" + maxJ + ", minJ=" + minJ);
	    //	    for (j=maxJ; j>=minJ; j--) {
	    for (j=minJ; j<=maxJ; j++) {
		if (j<J && j>=0) {
		    tempcost=frameDistance(reference.data[j], signal.data[i]);
		    //		    System.out.println(" j: " + j + ", df[i][j]: " + tempcost);
		    mink = -1;
		    minc = 1000000.0;
		    temp = 0.0;

		    for (k=j; k>=minJ; k--) {
			if (cost[i-1][k] >= 0.0) {
			    temp = cost[i-1][k];
			    if (minc >temp) {
				minc = temp;
				mink = k;
			    }
			}
		    }
		    
		    if (mink >= 0) {
			//			cost[i][j] = minc;
			cost[i][j] = minc + tempcost;
			path[i][j] = mink;
		    }
		    //		    System.out.println(" predecessor["+i+"]["+j+"]: " + path[i][j] +
		    //				       ", cost["+i+"]["+j+"]: " + cost[i][j]);
		}
	    }
	}   
	// back trace the best matching path
	backTrace(path, cost, I, J);
	return true;
    }
    
    // method to back trace the path 
    private void backTrace(int path2[][], double cost2[][], 
		      int testlength, int reflength) {
	int i, j;
	double min = 10000.0;
	int minX = 0;
	int minY = 0;
	int temppath[] = new int[testlength+1];
	int temppathlength = 0;

	minX = testlength -1;
	minY = reflength -1;
	
	j = minY;
	temppath[0] = minY;

	// trace the path from J-1 back to 0
	for (i=minX; i>=0; i--) {
	    j = path2[i][j];
	    temppathlength++;
	    temppath[temppathlength] = j;
	}

	// reverse the path
	for (i=0; i<=temppathlength/2; i++) {
	    j = temppath[i];
	    temppath[i] = temppath[temppathlength-i];
	    temppath[temppathlength-i] = j;
	}

	for (i=0; i<temppathlength; i++) {
	    temppath[i] = temppath[i+1];
	}

	// copy temppath to pathRecord[templateIndex]
	pathRecord[templateIndex] = temppath;
	pathLength[templateIndex] = temppathlength;
	
	// copy the total cost to totalCost array
	totalCost[templateIndex] = cost2[minX][minY];

	
	// copy accumulated cost to costRecord array
	for (i=0; i<testlength; i++) {
	    costRecord[i] = cost2[i][pathRecord[templateIndex][i]];
	}
    }

    /**
     * This method is used to recognize and
     * compute the matching path between test signal and each template and
     * find out the template with least cost
     */
    public void bestMatch() {
	double temp = 10000.0;
	for (templateIndex=0; templateIndex<num_of_templates; 
	     templateIndex++) {
	    setReference(templateIndex);
	    lefttorightMatch();

	    if (totalCost[templateIndex] < temp) {
		recogResult = templateIndex;
		temp = totalCost[templateIndex];
	    }
	}
	reference = template[recogResult];
    }

    /**
     * access method for setting main menu
     *
     * @param menu MainMenu object to set
     */
    public void setMenu(MainMenu menu) {
	control = menu;
    }
}
