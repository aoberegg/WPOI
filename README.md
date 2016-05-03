# WPOI

A weather context aware algorithm incorporated in MyMediaLite. (Zeno Gantner, Steffen Rendle, Christoph Freudenthaler, Lars Schmidt-Thieme:
MyMediaLite: A Free Recommender System Library. RecSys 2011)

The WPOI algorithm is implemented in:
WPOI/src/MyMediaLite/RatingPrediction/WeatherContextAwareItemRecommender.cs

An example how to use it can be found in:
WPOI/examples/csharp/TestMediaLite/TestMediaLite/rating_prediction.cs

Note that the algorithm is just runable when included in MyMediaLite (https://github.com/zenogantner/MyMediaLite).


This Section provides a short technical introduction into the code written for the WPOI algorithm. The basis of the implementation is the work of \cite{mymedialite} that resulted in the MyMediaLite\footnote{\url{https://github.com/zenogantner/MyMediaLite}\lastaccess}  project. The code from the WPOI project has to be integrated into the MyMediaLite data structure to work properly. The following steps have to be done to get the project to work:
\begin{enumerate}
\item Download the sql database dump from \url{https://github.com/aoberegg/WPOI/tree/master/database/final_database.rar} and create a database out of it. 
\item Checkout the MyMediaLite code \footnote{\url{https://github.com/zenogantner/MyMediaLite}\lastaccess}.
\item Checkout the WPOI code \footnote{\url{https://github.com/aoberegg/WPOI/}\lastaccess} and integrate it into the data structure of MyMediaLite (some of the files already exist, please overwrite).
\item Download Xamarin Studio\footnote{\url{https://www.xamarin.com/download}\lastaccess} to work with the project.
\end{enumerate}
\subsection*{Structure}
The application is separated into two parts, namely the MyMediaLite dll that can be found at 
\begin{lstlisting}
src/MyMediaLite/bin/Release
\end{lstlisting}
and the test program that is located at 
\begin{lstlisting}
examples/csharp/TestMediaLite/rating_prediction
\end{lstlisting}
\subsubsection*{MyMediaLite DLL}
To change the MyMediaLite DLL the project storet at 
\begin{lstlisting}[language=bash]
"src/MyMediaLite/MyMediaLite.sln"
\end{lstlisting}
has to be opened in Xamarin Studio. The WPOI algorithm is then implemented in 
\begin{lstlisting}
"src/MyMediatLite/RatingPrediction/WeatherContextAwareItemRecommender"
\end{lstlisting}
Please be aware that the weather data is retrieved from the database stored in the MySql dump.
\subsubsection*{WPOI Project}
The WPOI test project is based on the MyMediaLite DLL and is stored at 
\begin{lstlisting}[language=bash]
"examples/csharp/TestMediaLite/TestMediaLite.sln"
\end{lstlisting}
The test programm is implemented in 
\begin{lstlisting}[language=bash]
"examples/csharp/TestMediaLite/rating\_prediction.cs"
\end{lstlisting}
and provides options for testing the baseline algorithms as well as options for testing WPOI. the resulting executable stored at 
\begin{lstlisting}[language=bash]
"examples/csharp/TestMediaLite/bin/2/Release/TestMediaLite.exe"
\end{lstlisting}
can be executed with the following command line options:
\begin{lstlisting}[language=bash]
TestMediaLite.exe "path/to/check-in-data/filename.data" 
"SERVER=localhost;DATABASE=<database_name>;UID=<user_id>;
PASSWORD=<password>"
"<algorithm_id>"
"<city_id>"
"<max_iterations>"
"<weather_feature_in_database>"
\end{lstlisting}
A start of the program with the WPOI algorithm on city 53 with 7000 iterations and the temperature weather feature on Linux OS could look like follows:
\begin{lstlisting}[language=bash]
mono TestMediaLite.exe 
"/home/aoberegger/data/evaluation_protocol/city53.data" 
"SERVER=localhost;DATABASE=localhost;UID=root;PASSWORD=rootpw"
"4" "53" "7000" "hw.temperature"
\end{lstlisting}
Table \ref{tab:command_line_options} shows the command line options for the TestMediaLite executable.
\begin{table}[htb]
\begin{center}
\begin{tabular}{|p{4cm}|p{6cm}|}
\hline 
\textbf{Option} & \textbf{Possible values} \\ 
\hline 
"path/to/check-in-data/filename.data" & Path to the check-in data file containing <user\_id, item\_id, Timestamp> triples.\\
\hline 
"SERVER=localhost;\-DATABASE=localhost\-;UID=root\-;PASSWORD=rootpw"  &  The Database credentials.\\ 
\hline
city\_id &  Every city id available in the database but the id has to fit with the datafile containing the check-ins.\\ 
\hline 
max\_iterations & The maximum amount of iterations for learning the latent model parameters. \\ 
\hline 
weather\_feature\_in\_database & 1 = Beta tuning, 2 = Rank-GeoFM, 3 = Old version of WPOI, 4 = WPOI, 5 = MP, 6 = Item-KNN, 7 = User-KNN, 8 = WRMF, 9 = BPRMF \\
\hline 
\end{tabular} 
\end{center}
\caption{Command line options for the TestMediaLite executable.}
\label{tab:command_line_options}
\end{table}

