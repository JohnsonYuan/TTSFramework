   M S B A M L   `   `   `  ����K  GMicrosoft.Tts.UI, Version=0.0.0.0, Culture=neutral, PublicKeyToken=nullE'clr-namespace:Microsoft.Tts.UI.ControlsMicrosoft.Tts.UI.Controls  5      [2clr-namespace:Microsoft.Tts.UI.Controls.Trajectory$Microsoft.Tts.UI.Controls.Trajectory  5      D    >Microsoft.Tts.UI.Controls.Trajectory.TrajectoryInfoUserControl   5      \ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5      7local'clr-namespace:Microsoft.Tts.UI.Controls    5      IlocalControl2clr-namespace:Microsoft.Tts.UI.Controls.Trajectory    5      R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5      UserControl_1��  � 	Resources  5      :  4Microsoft.Tts.UI.Controls.Trajectory.DoubleConverter   DoubleConverter8   2Microsoft.Tts.UI.Controls.VisibilityHightConvertor  VisibilityHightConvertor��%�   &	        &	 3       5      local:DoubleConverter_1��5	        5
      $ local:VisibilityHightConvertor_1��5      5      .��5      �� Border_1��
���   �$��1q�5      ._�5      �� StackPanel_1��
�������.+�5      �� StackPanel_2�� �� Orientation$ 
Horizontal=�5      .+�5   
   �� TextBlock_18��$��10,0,0,0q�5       $
��bold"�5      [6    �� 5   
   TextBlock_1��$��14(�5      $��
2, 0, 2, 0q�5      $
��bold"�5      ��5      �� *Phone+�� 5    
   TextBlock_19��$
��bold"�5!       ]65   �� 5"   
   TextBlock_20��$��14(�5$      $��5, 0, 10, 0q�5%      ��5&      �� *Word+5'      �� 5(      
GroupBox_1��$��5,10, 5, 10q�5)   	   ��5*   
   �� 5+      TextBlock_2��
����  �Information5-   ,   .��5/   
   � 
Grid_1�� � RowDefinitions 50      �� 51      RowDefinition_1��66   �� 52      RowDefinition_2��66   �� 53      RowDefinition_3��66   �� 54      RowDefinition_4��66   �� 55      RowDefinition_5��66   �� 56      -   5         ��Name$ _candidatesParaRow��57      RowDefinition_6����58      �� *ShowCandidatesParameter+ �� 	Converter# [ 59       � ColumnDefinitions 5;      �� 5<      ColumnDefinition_1��$	��100 �6:   6H   �� 5=      ColumnDefinition_2��$��50 �6:   6G   .+�5?      �� TextBlock_16��$��0��6+   $��0��6;   Time:6O   �� 5@      TextBlock_3��$��0��6*   $��1��6:   Frame index:6U   �� 5A      TextBlock_21��$��0��6+   $��2��6;   Generated para.:6Z   �� 5B      TextBlock_23��$��0��6+   $��3��6;   Mean:6O   �� 5C      TextBlock_4��$��0��6*   $��4��6:   
Deviation:6S   �� 5D      TextBlock_5��$��0��6*   $��5��6:   Candidates para.:6Z   �� 5E      TextBlock_17��$��1��5G      $��0��5H      ��5I      �� *Time+# [  �� 5J      TextBlock_6��$��1��5L      $��1��5M      ��5N      �� *
FrameIndex+�� 5O      TextBlock_22��$��1��5Q      $��2��5R      ��5S      �� *GeneratedParameter+# [  �� 5T      TextBlock_7��$��1��5V      $��3��5W      ��5X      �� *Mean+# [  �� 5Y      TextBlock_8��$��1��5[      $��4��5\      ��5]      �� *StandardDeviation+# [  �� 5^      TextBlock_24��$��1��5`      $��5��5a      ��5b      �� *CandidatesParameter+# [  5c      5d   	   5f      5g      5h      