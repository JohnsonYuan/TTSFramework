   M S B A M L   `   `   `  ����  Microsoft.Tts.UIA    ;Microsoft.Tts.UI.Controls.Trajectory.LspErrorDetectionPanel   5      \ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5      R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5      UserControl_1��$	��150��5      $	��180��5      4  .System.Windows.Automation.AutomationProperties    Name$  LspErrorDetectionPanel��5       � 	Resources 5	       �� 
TargetType �� Property �� Value��%�  '��       '�� �     �� 5
      Style_1��" ��6   .�5      �� Setter_2�� �@�OverridesDefaultStyle . 6A   �� 5      Setter_1�� � ��$ 12(�64   �� 5      Setter_3�� � ��
 �   �66   5      �� 5      Style_2��" ��6   .�5      �� Setter_4�� �@�OverridesDefaultStyle . 6A   �� 5      Setter_5�� � ��$ 11(�64   �� 5      Setter_6�� � ��
 ��  �66   5      5      .��5      � 
Grid_1��$��5q�5       � RowDefinitions 5      �� 5      RowDefinition_1��$
��auto �6.   6>   �� 5      RowDefinition_2��$
��auto �6.   6>   �� 5      RowDefinition_3��$��* �6.   6;   �� 5      RowDefinition_4��$
��auto �6.   6>   �� 5      RowDefinition_5��$
��auto �6.   6>   .+�5      � 
Grid_2��$��0��5!       � ColumnDefinitions 5"      �� 5#   
   ColumnDefinition_1��$��80 �66   6C   �� 5$   
   ColumnDefinition_2��68    5&      �� 5'   
   RowDefinition_6��62   �� 5(   
   RowDefinition_7��62   �� 5)   
   RowDefinition_8��62   �� 5*   
   RowDefinition_9��62   �� 5+   
   RowDefinition_10��63   �� 5,   
   RowDefinition_11��63   �� 5-   
   RowDefinition_12��63   �� 5.   
   RowDefinition_13��63   .+�50      �� TextBlock_1��$��Center=�52   	   $��0��53   	   $��0��54   	   $��2��55   	   $��5q�56   	   Error Detection6%   �� 57      TextBlock_2��$��1��59   	   $��0��5:   	   LSP interval:6(   �� 5;      TextBlock_3��$��2��5=   	   $��0��5>   	   Threshold min:6)   �� 5?      TextBlock_4��$��3��5A   	   $��0��5B   	   Threshold max:6)   �� 5C      -   5         ��Name$ _dimensionIntervalTextBox��5E   	   _dimensionIntervalTextBox�� � ToolTip$, &The interval of two dimension indexes.��5F   	   $  DimensionIntervalTextBox��5G   	   $��1��5H   	   $��1��5I   	   $��Center=�5J   	   $��60��5K   	   6   �� 5L      -   5        $ _minThresholdTextBox��5N   	   _minThresholdTextBox��$6 0Errors with value less than this will be listed.��5O   	   $  MinThresholdTextBox��5P   	   $��2��5Q   	   $��1��5R   	   $��Center=�5S   	   $��60��5T   	   6   �� 5U      -   5        $ _maxThresholdTextBox��5W   	   _maxThresholdTextBox��$  MaxThresholdTextBox��5X   	   $8 2Errors with value larger than this will be listed.��5Y   	   $��3��5Z   	   $��1��5[   	   $��Center=�5\   	   $��60��5]   	   6   �� 5^      
CheckBox_1��$��5��5`   	   $��2��5a   	   $��
5, 0, 0, 0q�5c   	   	 d� 	IsChecked	 5b   	   �� *SortByFrame+.��5c      Sort by frame index62   �� 5d      -   5        
 ��Name$
 _errorTextBlock��5f   	   _errorTextBlock��$��6��5g   	   $��2��5h   	   $
��Wrap=�5i   	   
���  ��5j   	   �� 5k      -   5         ��Name$ _resetButton��5m   	   _resetButton��$��7��5n   	   $��0��5o   	   $��50��5p   	   $��Right=�5q   	   $��5q�5r   	   $��Reset��5s   	   5u   	   �� 5v      -   5        $ _findButton��5x   	   _findButton��$��7��5y   	   $��1��5z   	   $��50��5{   	   $��Right=�5|   	   $��5q�5}   	   $
��Find��5~   	   ��5�   
   �� 5�      Style_3�� �� Triggers 5�      �� 5�      DataTrigger_1�� �� Binding 62   ��  �� Path$ LoadingData� �� Value True6W   .a�5�      �� Setter_7�� � {� .  6W   5�      �� 5�      DataTrigger_2�� 62   �� $ LoadingData�	 False6W   .a�5�      �� Setter_8�� � {� . 6V   5�      5�      5�   	   5�      �� 5�      StackPanel_1�� �� Orientation$ 
Horizontal=�5�      $��Center=�5�      $��1��5�      .+�5�      �� -   5        $
 _errorCountTextBox��5�   $   _errorCountTextBox��$��5q�5�   	   ��5�   	   �� *
ErrorCount+ �� Mode$ OneWay=��� 5�      TextBlock_5��$��5q�5�      error(s) found68   5�      �� 5�      -   5        	ListBox_1��$��2��5�      $  ErrorEntryListBox��5�      ��5�      �� *	ErrorData+��5�      �� 5�   
   DataTemplate_2��.R�5�      �� TextBlock_6��$��12(�5�      ��5�   !   �� *
ErrorEntry+
 � Tag 5�      �� *
ErrorIndex+5�      5�      5�      �� 5�      
ListView_1��$��3��5�      $��Disabled=�5�      ��5�      �� *Pages+��5�      �� 5�   
   ItemsPanelTemplate_1��.R�5�      �� StackPanel_2��$��Right=�5�      $ 
Horizontal=�5�      6(   5�      ��5�      �� 5�   
   DataTemplate_3��.R�5�      �� TextBlock_7��$��2q�5�   !   �� 5�      -	   5        Hyperlink_1�� �� 
TargetName 5�      �� .�5�   &   .�5�      �� TextBlock_8����5�   %   �� 5�      5�      5�      5�      5�      5�      