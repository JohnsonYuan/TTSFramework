   M S B A M L   `   `   `  ����K  GMicrosoft.Tts.UI, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null[2clr-namespace:Microsoft.Tts.UI.Controls.Trajectory$Microsoft.Tts.UI.Controls.Trajectory  5      E'clr-namespace:Microsoft.Tts.UI.ControlsMicrosoft.Tts.UI.Controls  5      @    :Microsoft.Tts.UI.Controls.Trajectory.SingleTrajectoryGraph   5      \ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5      R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5      Ecustom2clr-namespace:Microsoft.Tts.UI.Controls.Trajectory     5      BcustomControls'clr-namespace:Microsoft.Tts.UI.Controls     5      -   5          >�Name$  _graphSystem��5      
_chart�� � 	Resources 5      3   -Microsoft.Tts.UI.Controls.VisibilityConvertor   VisibilityConvertor��%>   &	          5   
    custom:VisibilityConvertor_1��6R   5	      .��5
      � 
Grid_1��.+�5      >   8Microsoft.Tts.UI.Controls.Trajectory.TrajectoryFrameline  -   5          Name$ 
_frameline��5      	_timeline��y�5   	   ��  �� Path$ TimeAxis.FramelineShowed� �� 	Converter# [  ��5   	   �� $ ActualWidth� �� ElementName$ _graphSystem����5   	   �� $ ActualHeight�$ _graphSystem��C   =Microsoft.Tts.UI.Controls.Trajectory.TrajectoryHighlightFrame  5      -   5          Name$ _hightlighFrames��5   	   _hightlighFrames��@   :Microsoft.Tts.UI.Controls.Trajectory.IntervalConstantGraph  5      -   5          Name$ 
_meanGraph��5   	   
_meanGraph��   CurveStroke
 ��  �	   
Upperbound$		 1e9Q�5   	   
   
Lowerbound$

 -1e9Q�5   	   ��5   	   �� $ ActualWidth�$ _graphSystem����5   	   �� $ ActualHeight�$ _graphSystem��5   	   =   7Microsoft.Tts.UI.Controls.Trajectory.IntervalLinerGraph  5      -   5          Name$ _auxiliaryParameterGraph��5   	   _auxiliaryParameterGraph��   CurveStroke
 �  ��   CurveStrokeThickness$	 2.5Q�5   	      
Upperbound$	 1e9Q�5"   	      
Lowerbound$
 -1e9Q�5#   	   ��5    	   �� $ ActualWidth�$ _graphSystem����5!   	   �� $ ActualHeight�$ _graphSystem��5#   	     5$      -   5        $ _parameterGraph��5%   	   _linerGraph��   
IsEditable .  $	 2.5Q�5(   	   $	 1e9Q�5)   	   $
 -1e9Q�5*   	   ��5+   	   �� $ ActualWidth�$ _graphSystem����5,   	   �� $ ActualHeight�$ _graphSystem���� 5-      Rectangle_1��
���   � J� IsHitTestVisible .  ��5/      �� $ ActualWidth�$ _graphSystem����50      �� $ ActualHeight�$ _graphSystem��51      52      53      