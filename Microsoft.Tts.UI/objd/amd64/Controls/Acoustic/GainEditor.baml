   M S B A M L   `   `   `  ����K  GMicrosoft.Tts.UI, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null[2clr-namespace:Microsoft.Tts.UI.Controls.Trajectory$Microsoft.Tts.UI.Controls.Trajectory  5      3    -Microsoft.Tts.UI.Controls.Acoustic.GainEditor   5      \ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5      R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5      Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5      @d2http://schemas.microsoft.com/expression/blend/2008     5      OcustomTrajectory2clr-namespace:Microsoft.Tts.UI.Controls.Trajectory     5      UserControl_1��  � 	Resources  5	         GainEditorContextMenu��%9  &	        �� 5
   
   ContextMenu_1��.@�5      m� -   5        SelectFrames��$��SelectFrames��5   ,   6B   �� 5      Separator_1��6   m� 5      -   5        	ResetTune��$��	ResetTune��5   )   6<   m� 5      -   5        
DragFrames��$��
DragFrames��5   *   6>   5      5      .��5      � -   5         �Name$ _grid��5      	_grid�� � ContextMenu# [  6(   .+�5   
   C   =Microsoft.Tts.UI.Controls.Trajectory.TrajectoryHighlightFrame  -   5          Name$ _highlightFrames��5   &   _highlightFrames��5      =   7Microsoft.Tts.UI.Controls.Trajectory.IntervalLinerGraph  5   
   -   5          Name$ _guidelineGraph��5   %   _guidelineGraph��   
IsEditable .   J� IsHitTestVisible .     CurveStrokeThickness$ 2Q�5         CurveStroke
 ����	   
Upperbound$		 1e9Q�5      
   
Lowerbound$

 -1e9Q�5      5      �� 5   
   -   5         ��Name$ 	_gridRect��5      	_gridRect��
������  . 5    Q     5!   
   -   5        $ _linerGraph��5"   !   _linerGraph�� . $	 2.5Q�5$      $		 1e9Q�5%      $

 -1e9Q�5&      5'      �� 5(   
   Rectangle_1��
���   � .  67   5)      5*      